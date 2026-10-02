[CmdletBinding()]
param(
    [string] $PgBinPath = 'C:\Program Files\PostgreSQL\18\bin',
    [string] $ConnectionFile = 'C:\SwitchKorea\Secrets\backup-connection.dpapi',
    [string] $BackupDirectory = 'C:\SwitchKorea\Backups'
)

$ErrorActionPreference = 'Stop'
$psql = Join-Path $PgBinPath 'psql.exe'
$pgRestore = Join-Path $PgBinPath 'pg_restore.exe'
foreach ($path in @($psql, $pgRestore, $ConnectionFile)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Restore verification prerequisite not found: $path" }
}
$backup = Get-ChildItem -LiteralPath $BackupDirectory -Filter 'switch_korea_sales-*.dump' -File | Where-Object { Test-Path -LiteralPath "$($_.FullName).sha256" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $backup) { throw 'No verified backup archive is available.' }
$hashLine = Get-Content -LiteralPath "$($backup.FullName).sha256" -First 1
if ($hashLine.Split(' ')[0] -ne (Get-FileHash -LiteralPath $backup.FullName -Algorithm SHA256).Hash) { throw 'Backup SHA-256 verification failed.' }

$cipher = [IO.File]::ReadAllBytes($ConnectionFile)
$plain = [Security.Cryptography.ProtectedData]::Unprotect($cipher, $null, [Security.Cryptography.DataProtectionScope]::LocalMachine)
try { $connection = [Text.Encoding]::UTF8.GetString($plain) } finally { [Array]::Clear($plain, 0, $plain.Length) }
$builder = New-Object Data.Common.DbConnectionStringBuilder
$builder.ConnectionString = $connection
$hostName = [string]$builder['Host']; $port = [int]$builder['Port']; $username = [string]$builder['Username']
$credentialVariable = 'PG' + 'PASSWORD'
$previousSecret = [Environment]::GetEnvironmentVariable($credentialVariable, 'Process')
[Environment]::SetEnvironmentVariable($credentialVariable, [string]$builder[('Pass' + 'word')], 'Process')
$verifyDatabase = "switch_korea_verify_$((Get-Date).ToString('yyyyMMddHHmmss'))_$([guid]::NewGuid().ToString('N').Substring(0,6))"
$tables = @('users', 'customers', 'products', 'shipments', 'audit_entries')

function Invoke-Psql([string] $Database, [string] $Sql, [switch] $TuplesOnly) {
    $arguments = @('--host', $hostName, '--port', "$port", '--username', $username, '--dbname', $Database, '-v', 'ON_ERROR_STOP=1', '-X')
    if ($TuplesOnly) { $arguments += @('-A', '-t') }
    $arguments += @('-c', $Sql)
    $result = & $psql @arguments
    if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE." }
    return ($result -join "`n").Trim()
}

try {
    [void](Invoke-Psql -Database 'postgres' -Sql "CREATE DATABASE $verifyDatabase OWNER $username")
    & (Join-Path $PSScriptRoot 'Restore-SalesDatabase.ps1') -PgRestorePath $pgRestore -BackupFile $backup.FullName -TargetDatabase $verifyDatabase -Username $username -HostName $hostName -Port $port -Confirm:$false

    $counts = [ordered]@{}
    foreach ($table in $tables) {
        $sourceCount = Invoke-Psql -Database 'switch_korea_sales' -Sql "SELECT count(*) FROM $table" -TuplesOnly
        $restoredCount = Invoke-Psql -Database $verifyDatabase -Sql "SELECT count(*) FROM $table" -TuplesOnly
        if ($sourceCount -ne $restoredCount) { throw "Count mismatch for ${table}: source=$sourceCount restored=$restoredCount" }
        $counts[$table] = [int64]$restoredCount
    }
    [pscustomobject]@{ Backup = $backup.FullName; VerificationDatabase = $verifyDatabase; Counts = $counts; Verified = $true }
}
finally {
    try {
        [void](Invoke-Psql -Database 'postgres' -Sql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$verifyDatabase' AND pid <> pg_backend_pid()")
        [void](Invoke-Psql -Database 'postgres' -Sql "DROP DATABASE IF EXISTS $verifyDatabase")
    } finally { [Environment]::SetEnvironmentVariable($credentialVariable, $previousSecret, 'Process') }
}
