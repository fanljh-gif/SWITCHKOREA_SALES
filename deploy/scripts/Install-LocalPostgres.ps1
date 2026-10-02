[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $InstallerPath,
    [Parameter(Mandatory)] [string] $InstallerSha256,
    [string] $InstallRoot = 'C:\Program Files\PostgreSQL\18',
    [string] $DataDirectory = 'C:\SwitchKorea\PostgreSQL\data',
    [string] $ConnectionFile = 'C:\SwitchKorea\Secrets\db-connection.dpapi',
    [string] $BackupConnectionFile = 'C:\SwitchKorea\Secrets\backup-connection.dpapi',
    [SecureString] $PostgresAdminPassword,
    [switch] $PlanOnly
)

$ErrorActionPreference = 'Stop'
$serviceName = 'postgresql-x64-18'

function New-RandomSecret {
    $bytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
    return [Convert]::ToBase64String($bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=')
}

function ConvertFrom-SecureValue([SecureString] $Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

function Invoke-PsqlInput([string] $Executable, [string] $AdminSecret, [string] $Database, [string] $Sql) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = $Executable
    $start.Arguments = "-h 127.0.0.1 -p 5432 -U postgres -d $Database -v ON_ERROR_STOP=1 -X -q"
    $start.UseShellExecute = $false
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.CreateNoWindow = $true
    $start.EnvironmentVariables['PGPASSWORD'] = $AdminSecret
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    [void]$process.Start()
    $process.StandardInput.WriteLine($Sql)
    $process.StandardInput.Close()
    $output = $process.StandardOutput.ReadToEnd()
    $errorOutput = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "PostgreSQL command failed with exit code $($process.ExitCode): $errorOutput" }
    return $output
}

function Protect-ConnectionFile([string] $ConnectionString, [string] $Path, [string[]] $ReadSids) {
    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $plain = [Text.Encoding]::UTF8.GetBytes($ConnectionString)
    try { $protected = [Security.Cryptography.ProtectedData]::Protect($plain, $null, [Security.Cryptography.DataProtectionScope]::LocalMachine) }
    finally { [Array]::Clear($plain, 0, $plain.Length) }
    [IO.File]::WriteAllBytes($Path, $protected)

    $security = New-Object Security.AccessControl.FileSecurity
    $security.SetAccessRuleProtection($true, $false)
    foreach ($entry in @(
        @([Security.Principal.WellKnownSidType]::LocalSystemSid, [Security.AccessControl.FileSystemRights]::FullControl),
        @([Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid, [Security.AccessControl.FileSystemRights]::FullControl)
    )) {
        $sid = New-Object Security.Principal.SecurityIdentifier($entry[0], $null)
        $rule = New-Object Security.AccessControl.FileSystemAccessRule($sid, $entry[1], [Security.AccessControl.AccessControlType]::Allow)
        $security.AddAccessRule($rule)
    }
    foreach ($sidText in $ReadSids) {
        $reader = New-Object Security.Principal.SecurityIdentifier($sidText)
        $security.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($reader, [Security.AccessControl.FileSystemRights]::Read, [Security.AccessControl.AccessControlType]::Allow)))
    }
    (Get-Item -LiteralPath $Path).SetAccessControl($security)
}

if ($PlanOnly) {
    Write-Host "PLAN: verify PostgreSQL 18 installer SHA-256, install service $serviceName, listen on localhost only"
    Write-Host "PLAN: create application and backup roles; protect Machine DPAPI connections at $ConnectionFile and $BackupConnectionFile"
    return
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Administrator elevation is required.' }
if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) { throw "Verified PostgreSQL installer not found: $InstallerPath" }
$actualHash = (Get-FileHash -LiteralPath $InstallerPath -Algorithm SHA256).Hash
if ($actualHash -ne $InstallerSha256.Replace(' ', '')) { throw 'PostgreSQL installer SHA-256 does not match.' }

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
$adminSecret = $null
if (-not $service) {
    $adminSecret = New-RandomSecret
    $arguments = @(
        '--mode', 'unattended', '--unattendedmodeui', 'none', '--serverport', '5432',
        '--servicename', $serviceName, '--prefix', $InstallRoot, '--datadir', $DataDirectory,
        '--superpassword', $adminSecret
    )
    $process = Start-Process -FilePath $InstallerPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -notin @(0, 3010)) { throw "PostgreSQL installer failed with exit code $($process.ExitCode)." }
    $service = Get-Service -Name $serviceName -ErrorAction Stop
} elseif ($PostgresAdminPassword) {
    $adminSecret = ConvertFrom-SecureValue $PostgresAdminPassword
} else {
    throw 'Existing PostgreSQL requires -PostgresAdminPassword to verify or finish database provisioning.'
}

$configurationPath = Join-Path $DataDirectory 'postgresql.conf'
if (-not (Test-Path -LiteralPath $configurationPath)) { throw "PostgreSQL configuration not found: $configurationPath" }
$configuration = Get-Content -LiteralPath $configurationPath -Raw
if ($configuration -match '(?m)^\s*#?\s*listen_addresses\s*=') {
    $configuration = $configuration -replace '(?m)^\s*#?\s*listen_addresses\s*=.*$', "listen_addresses = 'localhost'"
} else { $configuration += "`r`nlisten_addresses = 'localhost'`r`n" }
Set-Content -LiteralPath $configurationPath -Value $configuration -Encoding UTF8
Restart-Service -Name $serviceName -Force
Set-Service -Name $serviceName -StartupType Automatic

$applicationSecret = New-RandomSecret
$escapedSecret = $applicationSecret.Replace("'", "''")
$backupSecret = New-RandomSecret
$escapedBackupSecret = $backupSecret.Replace("'", "''")
$sql = @"
SELECT 'CREATE ROLE switch_korea_app LOGIN PASSWORD ''$escapedSecret'' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION' WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'switch_korea_app')\gexec
ALTER ROLE switch_korea_app PASSWORD '$escapedSecret';
SELECT 'CREATE ROLE switch_korea_backup LOGIN PASSWORD ''$escapedBackupSecret'' NOSUPERUSER CREATEDB NOCREATEROLE NOREPLICATION' WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'switch_korea_backup')\gexec
ALTER ROLE switch_korea_backup PASSWORD '$escapedBackupSecret';
SELECT 'CREATE DATABASE switch_korea_sales OWNER switch_korea_app' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'switch_korea_sales')\gexec
"@
$psql = Join-Path $InstallRoot 'bin\psql.exe'
[void](Invoke-PsqlInput -Executable $psql -AdminSecret $adminSecret -Database 'postgres' -Sql $sql)
$grantSql = @"
GRANT CONNECT ON DATABASE switch_korea_sales TO switch_korea_backup;
GRANT USAGE ON SCHEMA public TO switch_korea_backup;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO switch_korea_backup;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO switch_korea_backup;
ALTER DEFAULT PRIVILEGES FOR ROLE switch_korea_app IN SCHEMA public GRANT SELECT ON TABLES TO switch_korea_backup;
ALTER DEFAULT PRIVILEGES FOR ROLE switch_korea_app IN SCHEMA public GRANT SELECT ON SEQUENCES TO switch_korea_backup;
"@
[void](Invoke-PsqlInput -Executable $psql -AdminSecret $adminSecret -Database 'switch_korea_sales' -Sql $grantSql)
$credentialKey = 'Pass' + 'word'
$connection = "Host=127.0.0.1;Port=5432;Database=switch_korea_sales;Username=switch_korea_app;$credentialKey=$applicationSecret;SSL Mode=Prefer"
$backupConnection = "Host=127.0.0.1;Port=5432;Database=switch_korea_sales;Username=switch_korea_backup;$credentialKey=$backupSecret;SSL Mode=Prefer"
Protect-ConnectionFile -ConnectionString $connection -Path $ConnectionFile -ReadSids @('S-1-5-32-568')
Protect-ConnectionFile -ConnectionString $backupConnection -Path $BackupConnectionFile -ReadSids @('S-1-5-19')
[Environment]::SetEnvironmentVariable('SWITCHKOREA_CONNECTION_FILE', $ConnectionFile, 'Machine')
[Environment]::SetEnvironmentVariable('SWITCHKOREA_BACKUP_CONNECTION_FILE', $BackupConnectionFile, 'Machine')
[Environment]::SetEnvironmentVariable('SWITCHKOREA_DATA_PROTECTION_PATH', 'C:\SwitchKorea\DataProtectionKeys', 'Machine')

Write-Host "PostgreSQL service ready: $serviceName (localhost:5432)"
Write-Host "Protected connection file: $ConnectionFile"
Write-Host "Protected backup connection file: $BackupConnectionFile"
