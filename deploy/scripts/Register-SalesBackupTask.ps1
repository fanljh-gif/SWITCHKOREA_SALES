[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [string] $TaskName = 'SwitchKorea-Sales-Backup',
    [string] $BackupScript,
    [string] $PgBinPath = 'C:\Program Files\PostgreSQL\18\bin',
    [string] $ConnectionFile = 'C:\SwitchKorea\Secrets\backup-connection.dpapi',
    [string] $BackupDirectory = 'C:\SwitchKorea\Backups',
    [string] $OperationsDirectory = 'C:\SwitchKorea\Operations'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($BackupScript)) { $BackupScript = Join-Path $PSScriptRoot 'Backup-SalesDatabase.ps1' }
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Administrator elevation is required.' }
foreach ($path in @($BackupScript, (Join-Path $PgBinPath 'pg_dump.exe'), (Join-Path $PgBinPath 'pg_restore.exe'), $ConnectionFile)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Backup prerequisite not found: $path" }
}

New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $OperationsDirectory -Force | Out-Null
$security = New-Object Security.AccessControl.DirectorySecurity
$security.SetAccessRuleProtection($true, $false)
foreach ($entry in @(
    @('S-1-5-18', [Security.AccessControl.FileSystemRights]::FullControl),
    @('S-1-5-32-544', [Security.AccessControl.FileSystemRights]::FullControl),
    @('S-1-5-19', [Security.AccessControl.FileSystemRights]::Modify)
)) {
    $sid = New-Object Security.Principal.SecurityIdentifier($entry[0])
    $rule = New-Object Security.AccessControl.FileSystemAccessRule($sid, $entry[1], [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit', [Security.AccessControl.PropagationFlags]::None, [Security.AccessControl.AccessControlType]::Allow)
    $security.AddAccessRule($rule)
}
(Get-Item -LiteralPath $BackupDirectory).SetAccessControl($security)

$operationSecurity = New-Object Security.AccessControl.DirectorySecurity
$operationSecurity.SetAccessRuleProtection($true, $false)
foreach ($entry in @(
    @('S-1-5-18', [Security.AccessControl.FileSystemRights]::FullControl),
    @('S-1-5-32-544', [Security.AccessControl.FileSystemRights]::FullControl),
    @('S-1-5-19', [Security.AccessControl.FileSystemRights]::ReadAndExecute)
)) {
    $sid = New-Object Security.Principal.SecurityIdentifier($entry[0])
    $rule = New-Object Security.AccessControl.FileSystemAccessRule($sid, $entry[1], [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit', [Security.AccessControl.PropagationFlags]::None, [Security.AccessControl.AccessControlType]::Allow)
    $operationSecurity.AddAccessRule($rule)
}
(Get-Item -LiteralPath $OperationsDirectory).SetAccessControl($operationSecurity)
$deployedBackupScript = Join-Path $OperationsDirectory 'Backup-SalesDatabase.ps1'
Copy-Item -LiteralPath $BackupScript -Destination $deployedBackupScript -Force

$arguments = @(
    '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', "`"$deployedBackupScript`"",
    '-PgDumpPath', "`"$(Join-Path $PgBinPath 'pg_dump.exe')`"",
    '-PgRestorePath', "`"$(Join-Path $PgBinPath 'pg_restore.exe')`"",
    '-ConnectionFile', "`"$ConnectionFile`"",
    '-BackupDirectory', "`"$BackupDirectory`"", '-RetentionDays', '30'
) -join ' '
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $arguments
$trigger = New-ScheduledTaskTrigger -Daily -At '02:00'
$taskPrincipal = New-ScheduledTaskPrincipal -UserId 'NT AUTHORITY\LOCAL SERVICE' -LogonType ServiceAccount -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 2)
$task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $taskPrincipal -Settings $settings
if ($PSCmdlet.ShouldProcess($TaskName, 'Register daily verified database backup at 02:00')) {
    Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null
    Write-Host "Scheduled task registered: $TaskName at 02:00 as LOCAL SERVICE"
}
