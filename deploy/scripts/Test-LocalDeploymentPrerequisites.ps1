[CmdletBinding()]
param(
    [string] $ServerIp,
    [string] $IntranetCidr,
    [switch] $DiscoverNetwork,
    [int] $MinimumFreeSpaceGb = 5
)

$ErrorActionPreference = 'Stop'

function Test-Administrator {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-ActiveIpv4Configuration {
    $configurations = @(Get-NetIPConfiguration -ErrorAction Stop | Where-Object {
        $_.NetAdapter.Status -eq 'Up' -and $_.IPv4DefaultGateway -and $_.IPv4Address -and
        $_.IPv4Address.IPAddress -notlike '169.254.*' -and $_.IPv4Address.IPAddress -ne '127.0.0.1'
    })
    if ($configurations.Count -ne 1) {
        throw "Ambiguous active IPAddress configuration: expected 1 adapter, found $($configurations.Count)."
    }
    return $configurations[0]
}

if ($DiscoverNetwork) {
    $network = Get-ActiveIpv4Configuration
    $ServerIp = $network.IPv4Address.IPAddress
    $prefixLength = $network.IPv4Address.PrefixLength
    $octets = $ServerIp.Split('.')
    if ($prefixLength -eq 24) { $IntranetCidr = "$($octets[0]).$($octets[1]).$($octets[2]).0/24" }
    else { throw "Automatic CIDR derivation supports /24 only; specify -IntranetCidr explicitly for /$prefixLength." }
}

if ([string]::IsNullOrWhiteSpace($ServerIp)) { throw 'IPAddress is required; specify -ServerIp or use -DiscoverNetwork.' }
if ([string]::IsNullOrWhiteSpace($IntranetCidr)) { throw 'IntranetCidr is required.' }
$parsedIp = $null
if (-not [Net.IPAddress]::TryParse($ServerIp, [ref]$parsedIp) -or $parsedIp.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork -or [Net.IPAddress]::IsLoopback($parsedIp)) {
    throw "Invalid non-loopback IPv4 address: $ServerIp"
}
if ($IntranetCidr -notmatch '^([^/]+)/(\d|[12]\d|3[0-2])$') { throw "Invalid IPv4 CIDR: $IntranetCidr" }
$cidrIp = $null
if (-not [Net.IPAddress]::TryParse($Matches[1], [ref]$cidrIp) -or $cidrIp.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork) {
    throw "Invalid IPv4 CIDR: $IntranetCidr"
}
if ($env:COMPUTERNAME -and $env:COMPUTERNAME -ne 'JHLEE') { throw "This installer is restricted to JHLEE; current host is $env:COMPUTERNAME." }
if (-not (Test-Administrator)) { throw 'Administrator elevation is required.' }

$systemDrive = Get-PSDrive -Name $env:SystemDrive.TrimEnd(':')
if ($systemDrive.Free -lt ($MinimumFreeSpaceGb * 1GB)) { throw "At least $MinimumFreeSpaceGb GB free space is required." }
$pendingReboot = (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending') -or
    (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired')
$conflicts = @(Get-NetTCPConnection -State Listen -LocalPort 80,443 -ErrorAction SilentlyContinue)

[pscustomobject]@{
    ComputerName = $env:COMPUTERNAME
    ServerIp = $ServerIp
    IntranetCidr = $IntranetCidr
    IsAdministrator = $true
    FreeSpaceGb = [math]::Round($systemDrive.Free / 1GB, 1)
    PendingReboot = [bool]$pendingReboot
    ExistingWebListeners = $conflicts.Count
    IisFeatureCommandAvailable = [bool](Get-Command Enable-WindowsOptionalFeature -ErrorAction SilentlyContinue)
}
