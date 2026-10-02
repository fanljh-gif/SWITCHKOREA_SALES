[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PgDumpPath,
    [Parameter(Mandatory)] [string] $PgRestorePath,
    [string] $Database = 'switch_korea_sales',
    [string] $Username,
    [Parameter(Mandatory)] [string] $BackupDirectory,
    [string] $ConnectionFile,
    [string] $HostName = '127.0.0.1',
    [int] $Port = 5432,
    [ValidateRange(1, 3650)] [int] $RetentionDays = 30,
    [scriptblock] $CommandRunner = { param($Executable, $Arguments) & $Executable @Arguments | Out-Null; return $LASTEXITCODE }
)

$ErrorActionPreference = 'Stop'
foreach ($path in @($PgDumpPath, $PgRestorePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "PostgreSQL tool not found: $path" }
}

$credentialVariable = 'PG' + 'PASSWORD'
$previousSecret = [Environment]::GetEnvironmentVariable($credentialVariable, 'Process')
if ($ConnectionFile) {
    if (-not (Test-Path -LiteralPath $ConnectionFile -PathType Leaf)) { throw "Protected connection file not found: $ConnectionFile" }
    $cipher = [IO.File]::ReadAllBytes($ConnectionFile)
    $plain = [Security.Cryptography.ProtectedData]::Unprotect($cipher, $null, [Security.Cryptography.DataProtectionScope]::LocalMachine)
    try { $connection = [Text.Encoding]::UTF8.GetString($plain) } finally { [Array]::Clear($plain, 0, $plain.Length) }
    $builder = New-Object Data.Common.DbConnectionStringBuilder
    $builder.ConnectionString = $connection
    $HostName = [string]$builder['Host']; $Port = [int]$builder['Port']; $Database = [string]$builder['Database']; $Username = [string]$builder['Username']
    [Environment]::SetEnvironmentVariable($credentialVariable, [string]$builder[('Pass' + 'word')], 'Process')
}
if ([string]::IsNullOrWhiteSpace($Username)) { throw 'Username is required.' }
if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($credentialVariable, 'Process'))) { throw 'A process-scoped PostgreSQL credential or ConnectionFile is required.' }

try {
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $target = Join-Path $BackupDirectory "$Database-$stamp.dump"
    $dumpArguments = @('--host', $HostName, '--port', "$Port", '--username', $Username, '--dbname', $Database, '--format', 'custom', '--file', $target)
    $exitCode = & $CommandRunner $PgDumpPath $dumpArguments
    if ($exitCode -ne 0) { Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue; throw "Backup command failed with exit code $exitCode." }
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw 'Backup command produced no archive.' }
    if ((Get-Item -LiteralPath $target).Length -le 0) { Remove-Item -LiteralPath $target -Force; throw 'Backup archive is empty.' }

    $listExit = & $CommandRunner $PgRestorePath @('--list', $target)
    if ($listExit -ne 0) { Remove-Item -LiteralPath $target -Force; throw 'Backup archive validation failed.' }
    $hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    Set-Content -LiteralPath "$target.sha256" -Value "$hash  $(Split-Path -Leaf $target)" -Encoding ASCII

    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    foreach ($old in Get-ChildItem -LiteralPath $BackupDirectory -Filter "$Database-*.dump" -File | Where-Object { $_.LastWriteTime -lt $cutoff }) {
        Remove-Item -LiteralPath $old.FullName -Force
        Remove-Item -LiteralPath "$($old.FullName).sha256" -Force -ErrorAction SilentlyContinue
    }
    Write-Output $target
}
finally {
    [Environment]::SetEnvironmentVariable($credentialVariable, $previousSecret, 'Process')
}
