[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$deployRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $deployRoot 'scripts\DeploymentState.psm1'
$installerPath = Join-Path $deployRoot 'scripts\Install-LocalSalesManagement.ps1'
$postgresPath = Join-Path $deployRoot 'scripts\Install-LocalPostgres.ps1'

foreach ($path in @($modulePath, $installerPath, $postgresPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing installer component: $path" }
}

Import-Module $modulePath -Force -DisableNameChecking

function Assert-Equal($Expected, $Actual, [string] $Message) {
    if (($Expected -join '|') -ne ($Actual -join '|')) { throw "$Message. Expected '$($Expected -join ',')', got '$($Actual -join ',')'." }
}
function Assert-True([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }

$expectedPhases = @('Prerequisites', 'PostgreSQL', 'Database', 'Migration', 'Accounts', 'Publish', 'Certificate', 'IIS', 'Firewall', 'Backup')
Assert-Equal $expectedPhases (Get-DeploymentPhases) 'Deployment phase order changed'

$statePath = Join-Path $env:TEMP "switch-korea-installer-$([guid]::NewGuid().ToString('N')).json"
try {
    $state = New-DeploymentState -Path $statePath
    Start-DeploymentPhase -State $state -Name 'Prerequisites'
    Complete-DeploymentPhase -State $state -Name 'Prerequisites'
    Save-DeploymentState -State $state -Path $statePath
    $reloaded = Read-DeploymentState -Path $statePath
    Assert-Equal 'PostgreSQL' (Get-NextDeploymentPhase -State $reloaded) 'Completed phases must be skipped'

    Start-DeploymentPhase -State $reloaded -Name 'PostgreSQL'
    $secretFailure = ('Pass' + 'word=secret-value; failed')
    Fail-DeploymentPhase -State $reloaded -Name 'PostgreSQL' -Message $secretFailure
    Assert-True ($reloaded.Phases.PostgreSQL.LastError -notmatch 'secret-value') 'Secrets must be redacted from state'
    Start-DeploymentPhase -State $reloaded -Name 'PostgreSQL'
    Assert-True ($reloaded.Phases.PostgreSQL.Attempts -eq 2) 'A failed phase must be retried and counted'

    $reloaded.PendingReboot = $true
    Save-DeploymentState -State $reloaded -Path $statePath
    Assert-True ((Read-DeploymentState -Path $statePath).PendingReboot) 'Pending reboot must persist before installer exit'
}
finally {
    Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
}

$planState = Join-Path $env:TEMP "switch-korea-plan-$([guid]::NewGuid().ToString('N')).json"
$plan = & powershell -NoProfile -ExecutionPolicy Bypass -File $installerPath -NetworkConfigPath 'C:\SwitchKorea\InstallState\network.json' -StatePath $planState -PlanOnly 2>&1
if ($LASTEXITCODE -ne 0) { throw "PlanOnly failed: $plan" }
Assert-True (-not (Test-Path -LiteralPath $planState)) 'PlanOnly must not create installer state'
$planText = $plan -join "`n"
$positions = @($expectedPhases | ForEach-Object { $planText.IndexOf($_) })
Assert-True (($positions | Where-Object { $_ -lt 0 }).Count -eq 0) 'PlanOnly must list every phase'
for ($index = 1; $index -lt $positions.Count; $index++) {
    Assert-True ($positions[$index] -gt $positions[$index - 1]) 'PlanOnly phases must be ordered'
}
Assert-True ($planText -notmatch '(?i)password\s*=\s*(?!<redacted>)') 'PlanOnly output must not expose passwords'

Write-Host 'Local installer tests passed.'
