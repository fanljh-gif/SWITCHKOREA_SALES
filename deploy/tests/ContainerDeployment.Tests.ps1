[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$required = @(
    (Join-Path $repoRoot 'Dockerfile'),
    (Join-Path $repoRoot '.dockerignore'),
    (Join-Path $repoRoot 'deploy\render\start.sh')
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required container artifact is missing: $path"
    }
}

$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    throw 'Docker is not installed. Run this test in Render or on a host with Docker before deployment approval.'
}

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 10)
$network = "switch-korea-test-$suffix"
$databaseContainer = "switch-korea-db-$suffix"
$appContainer = "switch-korea-app-$suffix"
$image = "switch-korea-sales-test:$suffix"
$fixturePassword = 'container-test-secret-9A!'
$port = 18766

function Invoke-Docker {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & $docker.Source @Arguments
    if ($LASTEXITCODE -ne 0) { throw "docker command failed: $($Arguments -join ' ')" }
}

try {
    Invoke-Docker network create $network | Out-Null
    Invoke-Docker build --tag $image $repoRoot
    Invoke-Docker run --detach --name $databaseContainer --network $network `
        --env "POSTGRES_DB=sales" --env "POSTGRES_USER=app" --env "POSTGRES_PASSWORD=$fixturePassword" `
        postgres:18-alpine | Out-Null

    $databaseReady = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        & $docker.Source exec $databaseContainer pg_isready -U app -d sales *> $null
        if ($LASTEXITCODE -eq 0) { $databaseReady = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $databaseReady) { throw 'Disposable PostgreSQL did not become ready.' }

    $databaseUrl = "postgresql://app:$fixturePassword@$databaseContainer`:5432/sales"
    Invoke-Docker run --detach --name $appContainer --network $network --publish "$port`:8080" `
        --env "DATABASE_URL=$databaseUrl" --env 'PORT=8080' `
        --env 'SWITCHKOREA_HOSTING_MODE=Render' --env 'RENDER_EXTERNAL_HOSTNAME=localhost' $image | Out-Null

    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$port/health/ready" -Headers @{ Host = 'localhost' }
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $ready) { throw 'Application readiness did not succeed.' }

    $root = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$port/" -Headers @{ Host = 'localhost' }
    if ($root.Content -notmatch '<div id="root">') { throw 'React entry page was not served.' }
    if ($root.Content -match [regex]::Escape($fixturePassword)) { throw 'A runtime secret appeared in the response.' }

    Invoke-Docker rm --force $appContainer | Out-Null
    Invoke-Docker run --detach --name $appContainer --network $network --publish "$port`:8080" `
        --env "DATABASE_URL=$databaseUrl" --env 'PORT=8080' `
        --env 'SWITCHKOREA_HOSTING_MODE=Render' --env 'RENDER_EXTERNAL_HOSTNAME=localhost' $image | Out-Null
    Start-Sleep -Seconds 3
    $restart = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$port/health/ready" -Headers @{ Host = 'localhost' }
    if ($restart.StatusCode -ne 200) { throw 'Readiness failed after restart.' }

    $history = (& $docker.Source history --no-trunc $image) -join "`n"
    if ($history -match [regex]::Escape($fixturePassword)) { throw 'A runtime secret appeared in image history.' }
    Write-Host 'Container deployment tests passed.'
}
finally {
    & $docker.Source rm --force $appContainer $databaseContainer *> $null
    & $docker.Source network rm $network *> $null
    & $docker.Source image rm $image *> $null
}
