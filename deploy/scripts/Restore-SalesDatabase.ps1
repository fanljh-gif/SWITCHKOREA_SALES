[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)] [string] $PgRestorePath,
    [Parameter(Mandatory)] [string] $BackupFile,
    [Parameter(Mandatory)] [string] $TargetDatabase,
    [Parameter(Mandatory)] [string] $Username,
    [string] $HostName = '127.0.0.1',
    [int] $Port = 5432,
    [switch] $AllowProductionRestore,
    [scriptblock] $CommandRunner = { param($Executable, $Arguments) & $Executable @Arguments | Out-Null; return $LASTEXITCODE }
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $PgRestorePath -PathType Leaf)) { throw "pg_restore not found: $PgRestorePath" }
if (-not (Test-Path -LiteralPath $BackupFile -PathType Leaf)) { throw "Backup file not found: $BackupFile" }
if ((Get-Item -LiteralPath $BackupFile).Length -le 0) { throw 'Backup file is empty.' }
if ([string]::IsNullOrWhiteSpace($env:PGPASSWORD)) { throw 'A process-scoped PostgreSQL credential is required.' }
$isProduction = $TargetDatabase -eq 'switch_korea_sales'
if ($isProduction -and -not $AllowProductionRestore) { throw 'Production restore requires -AllowProductionRestore and explicit ShouldProcess confirmation.' }

if ($PSCmdlet.ShouldProcess("$HostName/$TargetDatabase", "Restore verified backup '$BackupFile'")) {
    $arguments = @('--host', $HostName, '--port', "$Port", '--username', $Username, '--dbname', $TargetDatabase, '--clean', '--if-exists', '--single-transaction', '--no-owner', $BackupFile)
    $exitCode = & $CommandRunner $PgRestorePath $arguments
    if ($exitCode -ne 0) { throw "Restore failed with exit code $exitCode." }
}
