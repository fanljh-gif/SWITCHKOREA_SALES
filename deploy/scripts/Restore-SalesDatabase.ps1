[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)] [string] $PgRestorePath,
    [Parameter(Mandatory)] [string] $BackupFile,
    [Parameter(Mandatory)] [string] $TargetDatabase,
    [Parameter(Mandatory)] [string] $Username,
    [string] $HostName = 'localhost',
    [int] $Port = 5432
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $PgRestorePath -PathType Leaf)) { throw "pg_restore 실행 파일이 없습니다: $PgRestorePath" }
if (-not (Test-Path -LiteralPath $BackupFile -PathType Leaf)) { throw "복원 파일이 없습니다: $BackupFile" }
if ([string]::IsNullOrWhiteSpace($env:PGPASSWORD)) { throw '현재 프로세스에 PGPASSWORD가 설정되어야 합니다.' }
if ($PSCmdlet.ShouldProcess("$HostName/$TargetDatabase", "백업 '$BackupFile'을 clean/restore")) {
    & $PgRestorePath --host $HostName --port $Port --username $Username --dbname $TargetDatabase --clean --if-exists --single-transaction $BackupFile
    if ($LASTEXITCODE -ne 0) { throw "복원 실패(exit=$LASTEXITCODE)" }
}
