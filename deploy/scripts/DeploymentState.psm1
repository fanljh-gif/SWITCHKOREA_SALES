Set-StrictMode -Version 2.0
$script:DeploymentPhases = @('Prerequisites', 'PostgreSQL', 'Database', 'Migration', 'Accounts', 'Publish', 'Certificate', 'IIS', 'Firewall', 'Backup')

function Get-DeploymentPhases { return @($script:DeploymentPhases) }

function Protect-DeploymentText([string] $Text) {
    if ($null -eq $Text) { return $null }
    $result = $Text -replace '(?i)(password|pwd|pgpassword)\s*=\s*[^;\s]+', '$1=<redacted>'
    return $result -replace '(?i)(Host=[^;]+;[^\r\n]*Username=[^;]+;)[^\r\n]*', '$1<redacted>'
}

function New-DeploymentState([string] $Path) {
    $phases = [ordered]@{}
    foreach ($name in $script:DeploymentPhases) {
        $phases[$name] = [pscustomobject]@{ Status = 'Pending'; Attempts = 0; CompletedAt = $null; LastError = $null }
    }
    return [pscustomobject]@{
        Version = 1
        Path = $Path
        CreatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        UpdatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        PendingReboot = $false
        PendingRebootAt = $null
        Phases = [pscustomobject]$phases
    }
}

function Read-DeploymentState([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return New-DeploymentState -Path $Path }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Save-DeploymentState($State, [string] $Path) {
    $directory = Split-Path -Parent $Path
    if ($directory) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    $State.UpdatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    $temporary = "$Path.tmp"
    $State | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $Path -Force
}

function Get-PhaseState($State, [string] $Name) {
    if ($script:DeploymentPhases -notcontains $Name) { throw "Unknown deployment phase: $Name" }
    return $State.Phases.$Name
}

function Start-DeploymentPhase($State, [string] $Name) {
    $phase = Get-PhaseState -State $State -Name $Name
    if ($phase.Status -eq 'Completed') { return }
    $phase.Status = 'Running'
    $phase.Attempts = [int]$phase.Attempts + 1
    $phase.LastError = $null
}

function Complete-DeploymentPhase($State, [string] $Name) {
    $phase = Get-PhaseState -State $State -Name $Name
    $phase.Status = 'Completed'
    $phase.CompletedAt = [DateTimeOffset]::UtcNow.ToString('o')
    $phase.LastError = $null
}

function Fail-DeploymentPhase($State, [string] $Name, [string] $Message) {
    $phase = Get-PhaseState -State $State -Name $Name
    $phase.Status = 'Failed'
    $phase.LastError = Protect-DeploymentText $Message
}

function Get-NextDeploymentPhase($State) {
    foreach ($name in $script:DeploymentPhases) {
        if ((Get-PhaseState -State $State -Name $name).Status -ne 'Completed') { return $name }
    }
    return $null
}

Export-ModuleMember -Function Get-DeploymentPhases, Protect-DeploymentText, New-DeploymentState, Read-DeploymentState, Save-DeploymentState, Start-DeploymentPhase, Complete-DeploymentPhase, Fail-DeploymentPhase, Get-NextDeploymentPhase
