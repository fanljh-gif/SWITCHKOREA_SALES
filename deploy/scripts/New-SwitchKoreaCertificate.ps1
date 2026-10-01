[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ServerIp,
    [string] $DnsName = 'sales.switchkorea.local',
    [string] $ExportPath = 'C:\SwitchKorea\Certificates\SwitchKorea-RootCA.cer',
    [switch] $PlanOnly
)

$ErrorActionPreference = 'Stop'
$rootSubject = 'CN=SwitchKorea Root CA'
$serverSubject = "CN=$DnsName"

if ($PlanOnly) {
    [pscustomobject]@{ Action = 'CreateOrReuseCertificates'; DnsName = $DnsName; ServerIp = $ServerIp; PublicRootExport = $ExportPath }
    return
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Administrator elevation is required.' }
$parsedIp = $null
if (-not [Net.IPAddress]::TryParse($ServerIp, [ref]$parsedIp)) { throw "Invalid ServerIp: $ServerIp" }

$now = Get-Date
function Test-CertificateSan([Security.Cryptography.X509Certificates.X509Certificate2] $Certificate) {
    $san = $Certificate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.17' } | Select-Object -First 1
    if (-not $san) { return $false }
    $formatted = $san.Format($false)
    return $formatted -match [regex]::Escape($DnsName) -and $formatted -match [regex]::Escape($ServerIp)
}

$roots = @(Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq $rootSubject })
$root = $roots | Where-Object { $_.HasPrivateKey -and $_.NotAfter -gt $now.AddYears(1) } | Select-Object -First 1
if (-not $root -and $roots.Count -gt 0) { throw 'An existing SWITCH KOREA root certificate is invalid or expiring; replace it through an approved rotation procedure.' }
if (-not $root) {
    $root = New-SelfSignedCertificate -Type Custom -Subject $rootSubject -CertStoreLocation 'Cert:\LocalMachine\My' `
        -KeyAlgorithm RSA -KeyLength 4096 -HashAlgorithm SHA256 -KeyUsage CertSign,CRLSign,DigitalSignature `
        -KeyExportPolicy NonExportable -NotAfter $now.AddYears(10) `
        -TextExtension @('2.5.29.19={critical}{text}ca=TRUE&pathlength=1')
}

$servers = @(Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq $serverSubject })
$server = $servers | Where-Object {
    $_.HasPrivateKey -and $_.NotAfter -gt $now.AddMonths(3) -and (Test-CertificateSan $_)
} | Select-Object -First 1
if (-not $server -and $servers.Count -gt 0) { throw 'An existing sales server certificate has the wrong SAN, is invalid, or is expiring; use an approved rotation procedure.' }
if (-not $server) {
    $san = "2.5.29.17={text}DNS=$DnsName&IPAddress=$ServerIp"
    $server = New-SelfSignedCertificate -Type Custom -Subject $serverSubject -CertStoreLocation 'Cert:\LocalMachine\My' `
        -Signer $root -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 -KeyUsage DigitalSignature,KeyEncipherment `
        -KeyExportPolicy NonExportable -NotAfter $now.AddYears(2) `
        -TextExtension @($san, '2.5.29.37={text}1.3.6.1.5.5.7.3.1')
}

$exportDirectory = Split-Path -Parent $ExportPath
New-Item -ItemType Directory -Path $exportDirectory -Force | Out-Null
Export-Certificate -Cert $root -FilePath $ExportPath -Type CERT -Force | Out-Null
if (-not (Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Thumbprint -eq $root.Thumbprint })) {
    Import-Certificate -FilePath $ExportPath -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null
}

[pscustomobject]@{
    RootThumbprint = $root.Thumbprint
    ServerThumbprint = $server.Thumbprint
    DnsName = $DnsName
    ServerIp = $ServerIp
    PublicRootExport = $ExportPath
}
