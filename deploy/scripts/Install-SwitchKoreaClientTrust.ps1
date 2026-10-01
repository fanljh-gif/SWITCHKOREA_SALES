[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)] [string] $CertificatePath,
    [string] $ExpectedThumbprint
)

$ErrorActionPreference = 'Stop'
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Administrator elevation is required.' }
if (-not (Test-Path -LiteralPath $CertificatePath -PathType Leaf)) { throw "Certificate file not found: $CertificatePath" }

$certificate = New-Object Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath)
if ($certificate.HasPrivateKey) { throw 'Client trust package must not contain a private key.' }
if ($ExpectedThumbprint -and $certificate.Thumbprint -ne $ExpectedThumbprint.Replace(' ', '')) { throw 'Certificate thumbprint does not match the expected value.' }

if ($PSCmdlet.ShouldProcess('Cert:\LocalMachine\Root', "Trust SWITCH KOREA root certificate $($certificate.Thumbprint)")) {
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null
    $installed = Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Thumbprint -eq $certificate.Thumbprint }
    if (-not $installed) { throw 'Root certificate trust verification failed.' }
    Write-Host "Trusted root installed: $($certificate.Thumbprint)"
}
