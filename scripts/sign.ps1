[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
 [Parameter(Mandatory=$true)][string]$TimestampServer,
 [string]$SignToolPath = 'signtool.exe'
)
$ErrorActionPreference = 'Stop'
$exe = Join-Path (Split-Path $PSScriptRoot -Parent) 'dist/SnipRefine.exe'
$thumbprint = $CertificateThumbprint.Replace(' ','').ToUpperInvariant()
if ($thumbprint -notmatch '^[A-F0-9]{40}$') { throw 'Invalid certificate thumbprint.' }
$uri = [Uri]$TimestampServer
if (-not $uri.IsAbsoluteUri -or $uri.Scheme -notin @('http','https')) { throw 'Provide the timestamp URL documented by your certificate authority.' }
$cert = Get-Item "Cert:\CurrentUser\My\$thumbprint"
if (-not $cert.HasPrivateKey -or $cert.NotAfter -le (Get-Date) -or $cert.NotBefore -gt (Get-Date)) { throw 'Certificate has no private key or is outside its validity period.' }
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build the executable first.' }
& $SignToolPath sign /sha1 $thumbprint /fd SHA256 /tr $TimestampServer /td SHA256 $exe
if ($LASTEXITCODE -ne 0) { throw 'Signing failed; do not publish this executable.' }
& $SignToolPath verify /pa $exe
if ($LASTEXITCODE -ne 0) { throw 'Signature verification failed.' }
$signature = Get-AuthenticodeSignature -LiteralPath $exe
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $thumbprint -or -not $signature.TimeStamperCertificate) { throw 'Publisher or timestamp verification failed.' }
Write-Host 'Verified signed executable. Pack this exact executable without rebuilding.'
