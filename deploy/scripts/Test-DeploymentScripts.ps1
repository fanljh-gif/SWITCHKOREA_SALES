[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$required = @(
    'Test-LocalDeploymentPrerequisites.ps1',
    'New-SwitchKoreaCertificate.ps1',
    'Install-SwitchKoreaClientTrust.ps1',
    'Install-SalesManagement.ps1'
)
$failures = New-Object System.Collections.Generic.List[string]

foreach ($name in $required) {
    $path = Join-Path $scriptRoot $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $failures.Add("Missing script: $name")
        continue
    }
}

foreach ($script in Get-ChildItem -LiteralPath $scriptRoot -Filter '*.ps1') {
    $tokens = $null
    $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors)
    if ($errors.Count -gt 0) { $failures.Add("Parse errors in $($script.Name)") }
}

if ($failures.Count -eq 0) {
    $prerequisites = Get-Content -LiteralPath (Join-Path $scriptRoot 'Test-LocalDeploymentPrerequisites.ps1') -Raw
    if ($prerequisites -notmatch 'Ambiguous' -or $prerequisites -notmatch 'IPAddress') {
        $failures.Add('Prerequisite validation must reject missing or ambiguous IPv4 addresses.')
    }

    $certificate = Get-Content -LiteralPath (Join-Path $scriptRoot 'New-SwitchKoreaCertificate.ps1') -Raw
    if ($certificate -notmatch 'Export-Certificate' -or $certificate -match 'Export-PfxCertificate') {
        $failures.Add('Certificate export must contain the public root certificate only.')
    }

    $installer = Get-Content -LiteralPath (Join-Path $scriptRoot 'Install-SalesManagement.ps1') -Raw
    if ($installer -notmatch '\[switch\]\s*\$PlanOnly' -or $installer -notmatch "LocalPort\s+80" -or $installer -notmatch "LocalPort\s+443") {
        $failures.Add('Installer must support PlanOnly and CIDR-scoped ports 80 and 443.')
    }
    if ($installer -match 'LocalPort\s+5432') { $failures.Add('Installer must never open PostgreSQL port 5432.') }
    $returnIndex = $installer.IndexOf('if ($PlanOnly)')
    $mutationIndexes = @($installer.IndexOf('Import-Module WebAdministration'), $installer.IndexOf('New-NetFirewallRule'), $installer.IndexOf('Copy-Item')) | Where-Object { $_ -ge 0 }
    if ($returnIndex -lt 0 -or ($mutationIndexes | Where-Object { $_ -lt $returnIndex }).Count -gt 0) {
        $failures.Add('PlanOnly must return before IIS, firewall, certificate, or file mutations.')
    }

    $clientTrust = Get-Content -LiteralPath (Join-Path $scriptRoot 'Install-SwitchKoreaClientTrust.ps1') -Raw
    if ($clientTrust -notmatch 'Cert:\\LocalMachine\\Root' -or $clientTrust -notmatch 'Administrator') {
        $failures.Add('Client trust installer must require elevation and target LocalMachine Root.')
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Deployment script checks passed.'
