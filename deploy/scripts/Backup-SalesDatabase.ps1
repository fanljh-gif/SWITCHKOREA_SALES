[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PgDumpPath,
    [Parameter(Mandatory)] [string] $Database,
    [Parameter(Mandatory)] [string] $Username,
    [Parameter(Mandatory)] [string] $BackupDirectory,
    [string] $HostName = 'localhost',
    [int] $Port = 5432,
    [ValidateRange(1, 3650)] [int] $RetentionDays = 30
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $PgDumpPath -PathType Leaf)) { throw "pg_dump 실행 파일이 없습니다: $PgDumpPath" }
if ([string]::IsNullOrWhiteSpace($env:PGPASSWORD)) { throw '현재 프로세스에 PGPASSWORD가 설정되어야 합니다.' }
New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$target = Join-Path $BackupDirectory "$Database-$stamp.dump"
& $PgDumpPath --host $HostName --port $Port --username $Username --dbname $Database --format custom --file $target
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $target)) { throw "백업 실패(exit=$LASTEXITCODE)" }
Get-ChildItem -LiteralPath $BackupDirectory -Filter "$Database-*.dump" -File | Where-Object LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) | Remove-Item -Force
Write-Output $target
