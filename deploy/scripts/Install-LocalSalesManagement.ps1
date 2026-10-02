[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $NetworkConfigPath,
    [string] $SourceRoot,
    [string] $StatePath = 'C:\SwitchKorea\InstallState\state.json',
    [string] $PostgresInstallerPath,
    [string] $PostgresInstallerSha256,
    [SecureString] $PostgresAdminPassword,
    [switch] $PlanOnly
)

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot 'DeploymentState.psm1'
Import-Module $modulePath -Force -DisableNameChecking

if ($PlanOnly) {
    foreach ($phase in Get-DeploymentPhases) { Write-Host "PLAN: $phase" }
    Write-Host "PLAN: state=$StatePath network=$NetworkConfigPath secrets=<redacted>"
    return
}

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

function Unprotect-Connection([string] $Path) {
    $protected = [IO.File]::ReadAllBytes($Path)
    $plain = [Security.Cryptography.ProtectedData]::Unprotect($protected, $null, [Security.Cryptography.DataProtectionScope]::LocalMachine)
    try { return [Text.Encoding]::UTF8.GetString($plain) }
    finally { [Array]::Clear($plain, 0, $plain.Length) }
}

function Test-Administrator {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) { throw 'Administrator elevation is required. Rerun this same command from an elevated PowerShell.' }
if (-not (Test-Path -LiteralPath $NetworkConfigPath -PathType Leaf)) { throw "Network configuration not found: $NetworkConfigPath" }
$network = Get-Content -LiteralPath $NetworkConfigPath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($network.ServerIp) -or [string]::IsNullOrWhiteSpace($network.IntranetCidr)) { throw 'Network configuration requires ServerIp and IntranetCidr.' }
$address = Get-NetIPAddress -AddressFamily IPv4 -IPAddress $network.ServerIp -ErrorAction SilentlyContinue
if (-not $address) { throw "ServerIp is not assigned to this PC: $($network.ServerIp)" }
if ($address.PrefixOrigin -eq 'Dhcp' -and -not $network.DhcpReservationConfirmed) {
    throw 'The selected IPv4 address is dynamic. Set a static address or confirm a DHCP reservation in network.json.'
}

$state = Read-DeploymentState -Path $StatePath
if ($state.PendingReboot) {
    $lastBoot = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
    if ($lastBoot -le [DateTimeOffset]::Parse($state.PendingRebootAt).LocalDateTime) {
        throw 'A restart is still required. Restart JHLEE, then rerun this identical command.'
    }
    $state.PendingReboot = $false
    $state.PendingRebootAt = $null
    Save-DeploymentState -State $state -Path $StatePath
}

$connectionFile = 'C:\SwitchKorea\Secrets\db-connection.dpapi'
$certificateThumbprint = $null

foreach ($phase in Get-DeploymentPhases) {
    $phaseState = $state.Phases.$phase
    if ($phaseState.Status -eq 'Completed') { continue }
    Start-DeploymentPhase -State $state -Name $phase
    Save-DeploymentState -State $state -Path $StatePath
    Write-Host "START: $phase"
    try {
        switch ($phase) {
            'Prerequisites' {
                $result = & (Join-Path $PSScriptRoot 'Test-LocalDeploymentPrerequisites.ps1') -ServerIp $network.ServerIp -IntranetCidr $network.IntranetCidr
                if ($result.PendingReboot) {
                    $state.PendingReboot = $true
                    $state.PendingRebootAt = [DateTimeOffset]::Now.ToString('o')
                    Save-DeploymentState -State $state -Path $StatePath
                    throw 'A Windows restart is required. Restart JHLEE and rerun this identical command.'
                }
            }
            'PostgreSQL' {
                if ([string]::IsNullOrWhiteSpace($PostgresInstallerPath) -or [string]::IsNullOrWhiteSpace($PostgresInstallerSha256)) {
                    throw 'PostgresInstallerPath and PostgresInstallerSha256 are required for PostgreSQL provisioning.'
                }
                $postgresArguments = @{
                    InstallerPath = $PostgresInstallerPath
                    InstallerSha256 = $PostgresInstallerSha256
                    ConnectionFile = $connectionFile
                }
                if ($PostgresAdminPassword) { $postgresArguments[('PostgresAdmin' + 'Password')] = $PostgresAdminPassword }
                & (Join-Path $PSScriptRoot 'Install-LocalPostgres.ps1') @postgresArguments
            }
            'Database' {
                $service = Get-Service -Name 'postgresql-x64-18' -ErrorAction Stop
                if ($service.Status -ne 'Running') { throw 'PostgreSQL service is not running.' }
                if (-not (Test-Path -LiteralPath $connectionFile -PathType Leaf)) { throw 'Protected database connection file is missing.' }
            }
            'Migration' {
                $connection = Unprotect-Connection $connectionFile
                [Environment]::SetEnvironmentVariable('ConnectionStrings__SalesManagement', $connection, 'Process')
                try {
                    & dotnet run --project (Join-Path $SourceRoot 'src\backend\SalesManagement.Setup\SalesManagement.Setup.csproj') --no-launch-profile -- --migrate-only
                    if ($LASTEXITCODE -ne 0) { throw 'Database migration failed.' }
                } finally { [Environment]::SetEnvironmentVariable('ConnectionStrings__SalesManagement', $null, 'Process') }
            }
            'Accounts' {
                $connection = Unprotect-Connection $connectionFile
                [Environment]::SetEnvironmentVariable('ConnectionStrings__SalesManagement', $connection, 'Process')
                try {
                    & dotnet run --project (Join-Path $SourceRoot 'src\backend\SalesManagement.Setup\SalesManagement.Setup.csproj') --no-launch-profile -- --seed-only --credentials-path 'C:\SwitchKorea\Secrets\initial-credentials.txt'
                    if ($LASTEXITCODE -ne 0) { throw 'Initial account creation failed.' }
                } finally { [Environment]::SetEnvironmentVariable('ConnectionStrings__SalesManagement', $null, 'Process') }
            }
            'Publish' {
                & pnpm --dir (Join-Path $SourceRoot 'src\frontend') build
                if ($LASTEXITCODE -ne 0) { throw 'Front-end build failed.' }
                & dotnet publish (Join-Path $SourceRoot 'src\backend\SalesManagement.Api\SalesManagement.Api.csproj') -c Release -o (Join-Path $SourceRoot 'artifacts\publish')
                if ($LASTEXITCODE -ne 0) { throw 'Back-end publish failed.' }
            }
            'Certificate' {
                $certificate = & (Join-Path $PSScriptRoot 'New-SwitchKoreaCertificate.ps1') -ServerIp $network.ServerIp
                $certificateThumbprint = $certificate.ServerThumbprint
            }
            'IIS' {
                if (-not $certificateThumbprint) {
                    $certificateThumbprint = (Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq 'CN=sales.switchkorea.local' -and $_.HasPrivateKey } | Sort-Object NotAfter -Descending | Select-Object -First 1).Thumbprint
                }
                if (-not $certificateThumbprint) { throw 'Server certificate is unavailable.' }
                & (Join-Path $PSScriptRoot 'Install-SalesManagement.ps1') -SourceRoot $SourceRoot -ServerIp $network.ServerIp -IntranetCidr $network.IntranetCidr -CertificateThumbprint $certificateThumbprint -Confirm:$false
            }
            'Firewall' {
                foreach ($name in @('SwitchKorea-Sales HTTP Intranet', 'SwitchKorea-Sales HTTPS Intranet')) {
                    if (-not (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue)) { throw "Firewall rule missing: $name" }
                }
            }
            'Backup' {
                $register = Join-Path $PSScriptRoot 'Register-SalesBackupTask.ps1'
                if (-not (Test-Path -LiteralPath $register)) { throw 'Backup task registration script is not available yet.' }
                & $register
            }
        }
        Complete-DeploymentPhase -State $state -Name $phase
        Save-DeploymentState -State $state -Path $StatePath
        Write-Host "DONE: $phase"
    }
    catch {
        Fail-DeploymentPhase -State $state -Name $phase -Message $_.Exception.Message
        Save-DeploymentState -State $state -Path $StatePath
        throw "Phase $phase failed. Correct the blocking condition and rerun the identical command."
    }
}

Write-Host 'SWITCH KOREA local installation completed.'
