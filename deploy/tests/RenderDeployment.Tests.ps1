[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$blueprintPath = Join-Path $repoRoot 'render.yaml'
$dockerfilePath = Join-Path $repoRoot 'Dockerfile'
$bootstrapPath = Join-Path $repoRoot 'deploy\scripts\Initialize-RenderTestEnvironment.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Invoke-BootstrapProcess {
    param([string[]]$Arguments, [string]$DatabaseUrl)
    $previous = $env:RENDER_DATABASE_URL
    $previousErrorAction = $ErrorActionPreference
    try {
        $env:RENDER_DATABASE_URL = $DatabaseUrl
        $ErrorActionPreference = 'Continue'
        $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $bootstrapPath @Arguments 2>&1 | Out-String
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
        $env:RENDER_DATABASE_URL = $previous
    }
}

if (-not (Test-Path -LiteralPath $blueprintPath -PathType Leaf)) {
    throw "Render Blueprint is missing: $blueprintPath"
}
if (-not (Test-Path -LiteralPath $bootstrapPath -PathType Leaf)) {
    throw "Render bootstrap script is missing: $bootstrapPath"
}
if (-not (Test-Path -LiteralPath $dockerfilePath -PathType Leaf)) {
    throw "Render Dockerfile is missing: $dockerfilePath"
}

$yaml = Get-Content -Raw -LiteralPath $blueprintPath
$dockerfile = Get-Content -Raw -LiteralPath $dockerfilePath
Assert-True (($yaml | Select-String -Pattern '(?m)^\s*- type: web\s*$' -AllMatches).Matches.Count -eq 1) 'Blueprint must define exactly one web service.'
Assert-True (($yaml | Select-String -Pattern '(?m)^databases:\s*$' -AllMatches).Matches.Count -eq 1) 'Blueprint must define a databases section.'
Assert-True (($yaml | Select-String -Pattern '(?m)^\s*- name: switch-korea-sales-test-db\s*$' -AllMatches).Matches.Count -eq 1) 'Blueprint must define exactly one approved database.'
Assert-True ($yaml -match '(?m)^\s*name: switch-korea-sales-test\s*$') 'Web service name is incorrect.'
Assert-True ($yaml -match '(?m)^\s*runtime: docker\s*$') 'Web service must use the Docker runtime.'
Assert-True ($yaml -match '(?m)^\s*dockerfilePath: \./Dockerfile\s*$') 'Dockerfile path is incorrect.'
Assert-True ($yaml -match '(?m)^\s*healthCheckPath: /health/ready\s*$') 'Readiness health path is missing.'
Assert-True (($yaml | Select-String -Pattern '(?m)^\s*region: singapore\s*$' -AllMatches).Matches.Count -eq 2) 'Both resources must use Singapore.'
Assert-True (($yaml | Select-String -Pattern '(?m)^\s*plan: free\s*$' -AllMatches).Matches.Count -eq 2) 'Both resources must use the free plan.'
Assert-True ($yaml -match '(?m)^\s*postgresMajorVersion: [''"]?18[''"]?\s*$') 'PostgreSQL major version must be 18.'
Assert-True ($yaml -match '(?ms)key: DATABASE_URL\s+fromDatabase:\s+name: switch-korea-sales-test-db\s+property: connectionString') 'DATABASE_URL must come from the approved database.'
Assert-True ($yaml -match '(?ms)key: SWITCHKOREA_HOSTING_MODE\s+value: Render') 'Render hosting mode is missing.'
Assert-True ($yaml -notmatch 'RENDER_EXTERNAL_HOSTNAME\s*:|key:\s*RENDER_EXTERNAL_HOSTNAME') 'Platform-supplied hostname must not be hardcoded.'
Assert-True ($yaml -match '(?ms)key: Research__Provider\s+value: Disabled') 'Research provider must default to Disabled.'
Assert-True ($yaml -match '(?ms)key: Research__ApiKey\s+sync: false') 'Research API key must be declared as an unsynced secret.'
Assert-True ($yaml -match '(?ms)key: Research__MonthlyLimit\s+value: [''"]?100000[''"]?') 'Research monthly limit is missing.'
Assert-True ($dockerfile -match 'SalesManagement\.DbTests') 'Docker build must execute database tests.'
Assert-True ($dockerfile -match '(?m)^FROM mcr\.microsoft\.com/dotnet/sdk:10\.0 AS backend-build\s*$') 'Dockerfile must use the published .NET 10 SDK tag.'
Assert-True ($dockerfile -match '(?m)^FROM mcr\.microsoft\.com/dotnet/aspnet:10\.0 AS runtime\s*$') 'Dockerfile must use the published ASP.NET 10 runtime tag.'
Assert-True ($dockerfile -notmatch 'dotnet/(?:sdk|aspnet):10\.0-bookworm-slim') 'Dockerfile references an unavailable .NET 10 bookworm-slim tag.'

$render = Get-Command render -ErrorAction SilentlyContinue
if ($render) {
    & $render.Source blueprints validate $blueprintPath
    if ($LASTEXITCODE -ne 0) { throw 'Render CLI rejected render.yaml.' }
}

$testId = [Guid]::NewGuid().ToString('N')
$testRoot = Join-Path $env:TEMP "switch-korea-render-test-$testId"
$fakeRunner = Join-Path $testRoot 'fake-dotnet.ps1'
$markerPath = Join-Path $testRoot 'runner.json'
$credentialsPath = "C:\SwitchKorea\Secrets\render-test-$testId.txt"
$secretUrl = 'postgresql://render_user:NeverEcho-9A%21@db.example.test:5432/sales'

try {
    New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
    @'
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$RunnerArguments)
$credentialIndex = [Array]::IndexOf($RunnerArguments, '--credentials-path')
if ($credentialIndex -lt 0) { throw 'credentials path argument is missing' }
$credentialPath = $RunnerArguments[$credentialIndex + 1]
$marker = $env:SWITCHKOREA_TEST_MARKER
[pscustomobject]@{
    HasDatabaseUrl = -not [string]::IsNullOrWhiteSpace($env:DATABASE_URL)
    ArgumentContainsDatabaseUrl = ($RunnerArguments -contains $env:DATABASE_URL)
} | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8
if (Test-Path -LiteralPath $credentialPath) {
    Write-Output 'Initial accounts: created=0, existing=5'
    exit 0
}
New-Item -ItemType Directory -Path (Split-Path -Parent $credentialPath) -Force | Out-Null
@(
    ("jhlee`tTemp-A1!"),
    ("twpark`tTemp-B2!"),
    ("mcchoi`tTemp-C3!"),
    ("sylee`tTemp-D4!"),
    ("cslee`tTemp-E5!")
) | Set-Content -LiteralPath $credentialPath -Encoding UTF8
$currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$adminSid = New-Object System.Security.Principal.SecurityIdentifier('S-1-5-32-544')
$acl = New-Object System.Security.AccessControl.FileSecurity
$acl.SetAccessRuleProtection($true, $false)
$acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($currentSid, 'FullControl', 'Allow')))
$acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($adminSid, 'FullControl', 'Allow')))
Set-Acl -LiteralPath $credentialPath -AclObject $acl
Write-Output 'Initial accounts: created=5, existing=0'
exit 0
'@ | Set-Content -LiteralPath $fakeRunner -Encoding UTF8

    $missing = Invoke-BootstrapProcess -Arguments @('-DotNetPath', $fakeRunner, '-CredentialsPath', $credentialsPath) -DatabaseUrl ''
    Assert-True ($missing.ExitCode -ne 0) 'Bootstrap must reject a missing database URL.'

    $invalid = Invoke-BootstrapProcess -Arguments @('-DotNetPath', $fakeRunner, '-CredentialsPath', $credentialsPath) -DatabaseUrl 'https://example.test/not-a-database'
    Assert-True ($invalid.ExitCode -ne 0) 'Bootstrap must reject a non-PostgreSQL URL.'

    $previousMarker = $env:SWITCHKOREA_TEST_MARKER
    $env:SWITCHKOREA_TEST_MARKER = $markerPath
    try {
        $first = Invoke-BootstrapProcess -Arguments @('-DotNetPath', $fakeRunner, '-CredentialsPath', $credentialsPath) -DatabaseUrl $secretUrl
        Assert-True ($first.ExitCode -eq 0) "First bootstrap failed: $($first.Output)"
        Assert-True ($first.Output -notmatch [regex]::Escape($secretUrl)) 'Bootstrap output exposed the database URL.'
        Assert-True (Test-Path -LiteralPath $credentialsPath -PathType Leaf) 'Credential file was not created.'
        $runnerState = Get-Content -Raw -LiteralPath $markerPath | ConvertFrom-Json
        Assert-True $runnerState.HasDatabaseUrl 'Setup runner did not receive DATABASE_URL through its process environment.'
        Assert-True (-not $runnerState.ArgumentContainsDatabaseUrl) 'Database URL was passed as a command-line argument.'

        $ids = @(Get-Content -LiteralPath $credentialsPath | ForEach-Object { ($_ -split "`t", 2)[0] })
        Assert-True (($ids -join ',') -eq 'jhlee,twpark,mcchoi,sylee,cslee') 'Credential file does not contain the exact approved login IDs.'
        $beforeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $credentialsPath).Hash

        $verify = Invoke-BootstrapProcess -Arguments @('-DotNetPath', $fakeRunner, '-CredentialsPath', $credentialsPath, '-VerifyOnly') -DatabaseUrl $secretUrl
        Assert-True ($verify.ExitCode -eq 0) "Idempotence verification failed: $($verify.Output)"
        Assert-True ($verify.Output -match 'existing=5') 'Second run did not report five existing accounts.'
        Assert-True ($verify.Output -notmatch [regex]::Escape($secretUrl)) 'Verification output exposed the database URL.'
        $afterHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $credentialsPath).Hash
        Assert-True ($beforeHash -eq $afterHash) 'Second run changed the one-time credential file.'
    }
    finally {
        $env:SWITCHKOREA_TEST_MARKER = $previousMarker
    }

    Write-Host 'Render deployment tests passed.'
}
finally {
    if (Test-Path -LiteralPath $credentialsPath) { Remove-Item -LiteralPath $credentialsPath -Force }
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
