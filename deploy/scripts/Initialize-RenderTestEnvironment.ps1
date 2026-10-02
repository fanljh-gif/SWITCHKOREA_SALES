[CmdletBinding()]
param(
    [string]$DotNetPath = 'dotnet',
    [string]$CredentialsPath = 'C:\SwitchKorea\Secrets\render-test-initial-credentials.txt',
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
$approvedRoot = [System.IO.Path]::GetFullPath('C:\SwitchKorea\Secrets').TrimEnd('\') + '\'
$fullCredentialsPath = [System.IO.Path]::GetFullPath($CredentialsPath)
$approvedIds = @('jhlee', 'twpark', 'mcchoi', 'sylee', 'cslee')

function Test-CredentialAcl {
    param([string]$Path)

    $acl = Get-Acl -LiteralPath $Path
    if (-not $acl.AreAccessRulesProtected) {
        throw 'Credential file ACL inheritance is not disabled.'
    }

    $currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $allowedSids = @($currentSid, 'S-1-5-32-544')
    foreach ($rule in $acl.Access) {
        $sid = $rule.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value
        if ($rule.AccessControlType -ne 'Allow' -or $sid -notin $allowedSids) {
            throw 'Credential file ACL contains an unapproved access rule.'
        }
    }
}

function Test-CredentialIds {
    param([string]$Path)

    $ids = @(
        Get-Content -LiteralPath $Path | ForEach-Object {
            if ([string]::IsNullOrWhiteSpace($_) -or $_ -notmatch "`t") {
                throw 'Credential file format is invalid.'
            }
            ($_ -split "`t", 2)[0]
        }
    )
    if (($ids -join ',') -ne ($approvedIds -join ',')) {
        throw 'Credential file does not contain the exact approved login IDs.'
    }
}

if (-not $fullCredentialsPath.StartsWith($approvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Credential files must be written beneath C:\SwitchKorea\Secrets.'
}

$databaseUrl = $env:RENDER_DATABASE_URL
if ([string]::IsNullOrWhiteSpace($databaseUrl)) {
    throw 'RENDER_DATABASE_URL is required in the current process.'
}

$uri = $null
if (-not [System.Uri]::TryCreate($databaseUrl, [System.UriKind]::Absolute, [ref]$uri) -or
    $uri.Scheme -notin @('postgres', 'postgresql') -or
    [string]::IsNullOrWhiteSpace($uri.Host) -or
    [string]::IsNullOrWhiteSpace($uri.UserInfo) -or
    $uri.AbsolutePath -eq '/') {
    throw 'RENDER_DATABASE_URL must be a complete PostgreSQL URL.'
}

$exists = Test-Path -LiteralPath $fullCredentialsPath -PathType Leaf
if ($VerifyOnly -and -not $exists) {
    throw 'Verification requires the existing one-time credential file.'
}
if (-not $VerifyOnly -and $exists) {
    throw 'The one-time credential file already exists; refusing to overwrite it.'
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$setupProject = Join-Path $repoRoot 'src\backend\SalesManagement.Setup\SalesManagement.Setup.csproj'
$previousDatabaseUrl = $env:DATABASE_URL
try {
    $env:DATABASE_URL = $databaseUrl
    $runnerOutput = & $DotNetPath run --project $setupProject --configuration Release --no-launch-profile -- `
        --migrate --credentials-path $fullCredentialsPath 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw 'Render account bootstrap failed. Review the database availability and local setup prerequisites.'
    }
}
finally {
    $env:DATABASE_URL = $previousDatabaseUrl
    $databaseUrl = $null
}

$summary = [regex]::Match($runnerOutput, 'Initial accounts:\s*created=(\d+),\s*existing=(\d+)')
if (-not $summary.Success) {
    throw 'Setup completed without the expected account summary.'
}

$created = [int]$summary.Groups[1].Value
$existing = [int]$summary.Groups[2].Value
if ($VerifyOnly) {
    if ($created -ne 0 -or $existing -ne 5) {
        throw 'Idempotence verification expected created=0 and existing=5.'
    }
}
elseif ($created -ne 5 -or $existing -ne 0) {
    throw 'Initial bootstrap expected created=5 and existing=0.'
}

if (-not (Test-Path -LiteralPath $fullCredentialsPath -PathType Leaf)) {
    throw 'The expected one-time credential file was not created.'
}
Test-CredentialIds -Path $fullCredentialsPath
Test-CredentialAcl -Path $fullCredentialsPath

Write-Output "Render account bootstrap verified: created=$created, existing=$existing."
Write-Output "Credentials are stored in the approved local secrets directory."
