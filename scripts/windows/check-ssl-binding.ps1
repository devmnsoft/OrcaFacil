param([Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9.-]+$')][string]$HostName)
$ErrorActionPreference = 'Stop'
$client = [Net.Sockets.TcpClient]::new($HostName,443)
try {
  $ssl = [Net.Security.SslStream]::new($client.GetStream(),$false,({$true}))
  $ssl.AuthenticateAsClient($HostName)
  $cert = [Security.Cryptography.X509Certificates.X509Certificate2]::new($ssl.RemoteCertificate)
  [pscustomobject]@{ Subject=$cert.Subject; Issuer=$cert.Issuer; NotBefore=$cert.NotBefore; NotAfter=$cert.NotAfter; Thumbprint=$cert.Thumbprint }
} finally { if ($ssl) { $ssl.Dispose() }; $client.Dispose() }
