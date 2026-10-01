[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)] [string] $SourceRoot,
    [Parameter(Mandatory)] [string] $InstallPath,
    [Parameter(Mandatory)] [string] $HostName,
    [Parameter(Mandatory)] [string] $IntranetCidr,
    [string] $SiteName = 'SwitchKorea-Sales',
    [string] $AppPoolName = 'SwitchKorea-Sales',
    [int] $Port = 80,
    [string] $CertificateThumbprint
)

$ErrorActionPreference = 'Stop'
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw '관리자 권한 PowerShell이 필요합니다.' }
if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) { throw "소스 폴더가 없습니다: $SourceRoot" }
if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('ConnectionStrings__SalesManagement', 'Machine'))) { throw 'Machine 환경변수 ConnectionStrings__SalesManagement를 먼저 보호된 방식으로 설정하세요.' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET Hosting Bundle 또는 SDK가 필요합니다.' }
if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) { throw 'Front-end 빌드를 위한 pnpm이 필요합니다.' }

$frontend = Join-Path $SourceRoot 'src\frontend'
$backend = Join-Path $SourceRoot 'src\backend\SalesManagement.Api\SalesManagement.Api.csproj'
$publish = Join-Path $SourceRoot 'artifacts\publish'
if ($PSCmdlet.ShouldProcess($SiteName, '빌드하고 IIS 사이트를 설치 또는 갱신')) {
    & pnpm --dir $frontend build
    if ($LASTEXITCODE -ne 0) { throw 'Front-end 빌드 실패' }
    & dotnet publish $backend -c Release -o $publish
    if ($LASTEXITCODE -ne 0) { throw 'Back-end publish 실패' }
    $wwwroot = Join-Path $publish 'wwwroot'; New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
    Copy-Item -Path (Join-Path $frontend 'dist\*') -Destination $wwwroot -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $SourceRoot 'deploy\iis\web.config') -Destination (Join-Path $publish 'web.config') -Force

    Import-Module WebAdministration
    if (Test-Path "IIS:\Sites\$SiteName") { Stop-Website -Name $SiteName }
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Copy-Item -Path (Join-Path $publish '*') -Destination $InstallPath -Recurse -Force
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) { New-WebAppPool -Name $AppPoolName | Out-Null }
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value 'AlwaysRunning'
    if (-not (Test-Path "IIS:\Sites\$SiteName")) { New-Website -Name $SiteName -PhysicalPath $InstallPath -Port $Port -HostHeader $HostName -ApplicationPool $AppPoolName | Out-Null }
    if ($CertificateThumbprint) {
        New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $HostName -SslFlags 1 -ErrorAction SilentlyContinue
        Push-Location IIS:\SslBindings; try { Get-Item "Cert:\LocalMachine\My\$CertificateThumbprint" | New-Item "0.0.0.0!443!$HostName" -Force | Out-Null } finally { Pop-Location }
    }
    Get-NetFirewallRule -DisplayName "$SiteName Intranet" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    $firewallPort = if ($CertificateThumbprint) { 443 } else { $Port }
    New-NetFirewallRule -DisplayName "$SiteName Intranet" -Direction Inbound -Action Allow -Protocol TCP -LocalPort $firewallPort -RemoteAddress $IntranetCidr | Out-Null
    Start-Website -Name $SiteName
}
