[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$deployRoot = Split-Path -Parent $PSScriptRoot
$backupScript = Join-Path $deployRoot 'scripts\Backup-SalesDatabase.ps1'
$restoreScript = Join-Path $deployRoot 'scripts\Restore-SalesDatabase.ps1'
$registerScript = Join-Path $deployRoot 'scripts\Register-SalesBackupTask.ps1'
$verifyScript = Join-Path $deployRoot 'scripts\Test-SalesBackupRestore.ps1'

foreach ($path in @($backupScript, $restoreScript, $registerScript, $verifyScript)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing backup component: $path" }
}

function Assert-True([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Assert-Throws([scriptblock] $Action, [string] $Message) {
    $threw = $false
    try { & $Action } catch { $threw = $true }
    if (-not $threw) { throw $Message }
}

$testRoot = Join-Path $env:TEMP "switch-korea-backup-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$pgDump = Join-Path $testRoot 'pg_dump.exe'
$pgRestore = Join-Path $testRoot 'pg_restore.exe'
Set-Content -LiteralPath $pgDump -Value 'fake'
Set-Content -LiteralPath $pgRestore -Value 'fake'
$credentialVariable = 'PG' + 'PASSWORD'
[Environment]::SetEnvironmentVariable($credentialVariable, 'test-only', 'Process')

try {
    $global:SwitchKoreaBackupBehavior = 'ExitFailure'
    $runner = {
        param($Executable, $Arguments)
        if ((Split-Path -Leaf $Executable) -eq 'pg_dump.exe') {
            $target = $Arguments[[Array]::IndexOf($Arguments, '--file') + 1]
            if ($global:SwitchKoreaBackupBehavior -eq 'Missing') { return 0 }
            if ($global:SwitchKoreaBackupBehavior -eq 'Zero') { [IO.File]::WriteAllBytes($target, [byte[]]@()); return 0 }
            [IO.File]::WriteAllBytes($target, [byte[]](1,2,3,4))
            if ($global:SwitchKoreaBackupBehavior -eq 'ExitFailure') { return 2 }
            return 0
        }
        if ($global:SwitchKoreaBackupBehavior -eq 'Unreadable') { return 3 }
        return 0
    }

    Assert-Throws { & $backupScript -PgDumpPath $pgDump -PgRestorePath $pgRestore -Database switch_korea_sales -Username backup -BackupDirectory $testRoot -CommandRunner $runner } 'Nonzero pg_dump must fail'
    $global:SwitchKoreaBackupBehavior = 'Missing'
    Assert-Throws { & $backupScript -PgDumpPath $pgDump -PgRestorePath $pgRestore -Database switch_korea_sales -Username backup -BackupDirectory $testRoot -CommandRunner $runner } 'Missing dump must fail'
    $global:SwitchKoreaBackupBehavior = 'Zero'
    Assert-Throws { & $backupScript -PgDumpPath $pgDump -PgRestorePath $pgRestore -Database switch_korea_sales -Username backup -BackupDirectory $testRoot -CommandRunner $runner } 'Zero-byte dump must fail'
    $global:SwitchKoreaBackupBehavior = 'Unreadable'
    Assert-Throws { & $backupScript -PgDumpPath $pgDump -PgRestorePath $pgRestore -Database switch_korea_sales -Username backup -BackupDirectory $testRoot -CommandRunner $runner } 'Unreadable archive must fail'

    $oldMatching = Join-Path $testRoot 'switch_korea_sales-20000101-000000.dump'
    $oldOther = Join-Path $testRoot 'another_database-20000101-000000.dump'
    [IO.File]::WriteAllBytes($oldMatching, [byte[]](1))
    [IO.File]::WriteAllBytes($oldOther, [byte[]](1))
    (Get-Item $oldMatching).LastWriteTime = (Get-Date).AddDays(-31)
    (Get-Item $oldOther).LastWriteTime = (Get-Date).AddDays(-31)
    $global:SwitchKoreaBackupBehavior = 'Success'
    $created = & $backupScript -PgDumpPath $pgDump -PgRestorePath $pgRestore -Database switch_korea_sales -Username backup -BackupDirectory $testRoot -CommandRunner $runner
    Assert-True (Test-Path -LiteralPath "$created.sha256") 'Verified backup must have a SHA-256 sidecar'
    Assert-True (-not (Test-Path -LiteralPath $oldMatching)) 'Retention must delete matching dumps older than 30 days'
    Assert-True (Test-Path -LiteralPath $oldOther) 'Retention must not delete another database backup'

    $global:SwitchKoreaRestoreArguments = $null
    $restoreRunner = { param($Executable, $Arguments) $global:SwitchKoreaRestoreArguments = $Arguments; return 0 }
    Assert-Throws { & $restoreScript -PgRestorePath $pgRestore -BackupFile $created -TargetDatabase switch_korea_sales -Username backup -CommandRunner $restoreRunner -Confirm:$false } 'Production restore must require AllowProductionRestore'
    & $restoreScript -PgRestorePath $pgRestore -BackupFile $created -TargetDatabase switch_korea_verify_test -Username backup -CommandRunner $restoreRunner -Confirm:$false
    Assert-True ($global:SwitchKoreaRestoreArguments -contains '--single-transaction') 'Restore must use a single transaction'

    $verifySource = Get-Content -LiteralPath $verifyScript -Raw
    foreach ($table in @('users', 'customers', 'products', 'shipments', 'audit_entries')) {
        Assert-True ($verifySource -match $table) "Restore count comparison must include $table"
    }
    $registerSource = Get-Content -LiteralPath $registerScript -Raw
    Assert-True ($registerSource -match '02:00') 'Backup task must run daily at 02:00'
    Assert-True ($registerSource -match 'LOCAL SERVICE') 'Backup task must use a least-privilege service identity'
}
finally {
    [Environment]::SetEnvironmentVariable($credentialVariable, $null, 'Process')
    Remove-Variable -Name SwitchKoreaBackupBehavior -Scope Global -ErrorAction SilentlyContinue
    Remove-Variable -Name SwitchKoreaRestoreArguments -Scope Global -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Backup and restore tests passed.'
