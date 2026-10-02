[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)] [string] $SourceRoot,
    [string] $InstallPath = 'C:\SwitchKorea\SalesManagement',
    [string] $HostName = 'sales.switchkorea.local',
    [Parameter(Mandatory)] [string] $ServerIp,
    [Parameter(Mandatory)] [string] $IntranetCidr,
    [Parameter(Mandatory)] [string] $CertificateThumbprint,
    [string] $SiteName = 'SwitchKorea-Sales',
    [string] $AppPoolName = 'SwitchKorea-Sales',
    [switch] $PlanOnly
)

$ErrorActionPreference = 'Stop'

if ($PlanOnly) {
    @(
        "Validate JHLEE, IPv4 $ServerIp, CIDR $IntranetCidr, disk, IIS, and restart state",
        'Build React and publish ASP.NET Core API',
        "Deploy to $InstallPath",
        "Bind HTTP 80 and HTTPS 443 for $HostName using certificate $CertificateThumbprint",
        "Allow TCP 80 and 443 only from $IntranetCidr",
        'Do not create a PostgreSQL 5432 firewall rule'
    ) | ForEach-Object { Write-Host "PLAN: $_" }
    return
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Administrator elevation is required.' }
if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) { throw "SourceRoot not found: $SourceRoot" }
$directConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__SalesManagement', 'Machine')
$protectedConnectionFile = [Environment]::GetEnvironmentVariable('SWITCHKOREA_CONNECTION_FILE', 'Machine')
if ([string]::IsNullOrWhiteSpace($directConnection) -and [string]::IsNullOrWhiteSpace($protectedConnectionFile)) { throw 'Protected Machine connection setting is required.' }
if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('SWITCHKOREA_DATA_PROTECTION_PATH', 'Machine'))) { throw 'Machine data-protection path setting is required.' }
$certificate = Get-Item "Cert:\LocalMachine\My\$CertificateThumbprint" -ErrorAction Stop
if (-not $certificate.HasPrivateKey) { throw 'The server certificate private key is unavailable.' }

& (Join-Path $PSScriptRoot 'Test-LocalDeploymentPrerequisites.ps1') -ServerIp $ServerIp -IntranetCidr $IntranetCidr | Out-Null
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET Hosting Bundle or SDK is required.' }
if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) { throw 'pnpm is required for the front-end build.' }

if ($PSCmdlet.ShouldProcess($SiteName, 'Build and configure the IIS site')) {
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-StaticContent,IIS-DefaultDocument,IIS-HttpRedirect,IIS-ManagementConsole -All -NoRestart | Out-Null

    $frontend = Join-Path $SourceRoot 'src\frontend'
    $backend = Join-Path $SourceRoot 'src\backend\SalesManagement.Api\SalesManagement.Api.csproj'
    $publish = Join-Path $SourceRoot 'artifacts\publish'
    & pnpm --dir $frontend build
    if ($LASTEXITCODE -ne 0) { throw 'Front-end build failed.' }
    & dotnet publish $backend -c Release -o $publish
    if ($LASTEXITCODE -ne 0) { throw 'Back-end publish failed.' }
    $wwwroot = Join-Path $publish 'wwwroot'
    New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
    Copy-Item -Path (Join-Path $frontend 'dist\*') -Destination $wwwroot -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $SourceRoot 'deploy\iis\web.config') -Destination (Join-Path $publish 'web.config') -Force

    Import-Module WebAdministration
    if (Test-Path "IIS:\Sites\$SiteName") { Stop-Website -Name $SiteName }
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Copy-Item -Path (Join-Path $publish '*') -Destination $InstallPath -Recurse -Force
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) { New-WebAppPool -Name $AppPoolName | Out-Null }
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value 'AlwaysRunning'
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name autoStart -Value $true
    if (-not (Test-Path "IIS:\Sites\$SiteName")) {
        New-Website -Name $SiteName -PhysicalPath $InstallPath -Port 80 -HostHeader $HostName -ApplicationPool $AppPoolName | Out-Null
    }
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationDefaults.preloadEnabled -Value $true
    if (-not (Get-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $HostName -ErrorAction SilentlyContinue)) {
        New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $HostName -SslFlags 1 | Out-Null
    }
    Push-Location IIS:\SslBindings
    try { Get-Item "Cert:\LocalMachine\My\$CertificateThumbprint" | New-Item "0.0.0.0!443!$HostName" -Force | Out-Null }
    finally { Pop-Location }

    Get-NetFirewallRule -DisplayName "$SiteName HTTP Intranet" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    Get-NetFirewallRule -DisplayName "$SiteName HTTPS Intranet" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName "$SiteName HTTP Intranet" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80 -RemoteAddress $IntranetCidr | Out-Null
    New-NetFirewallRule -DisplayName "$SiteName HTTPS Intranet" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443 -RemoteAddress $IntranetCidr | Out-Null
    Start-Website -Name $SiteName
}
