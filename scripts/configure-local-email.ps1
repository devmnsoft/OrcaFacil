$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root 'src/OrcaFacil.Web/appsettings.Local.json'
$example = Join-Path $root 'src/OrcaFacil.Web/appsettings.Local.example.json'
if (-not (Test-Path $target)) { Copy-Item $example $target }
$config = Get-Content $target -Raw | ConvertFrom-Json
$address = Read-Host 'Endereço remetente [comercial@mnsoft.com.br]'
if ([string]::IsNullOrWhiteSpace($address)) { $address = 'comercial@mnsoft.com.br' }
$securePassword = Read-Host 'Nova senha de app do Gmail' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try { $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
  $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
  function New-Pepper { $bytes = New-Object byte[] 48; $rng.GetBytes($bytes); [Convert]::ToBase64String($bytes) }
  $config | Add-Member NoteProperty Email ([pscustomobject]@{Provider='GmailSmtp';Host='smtp.gmail.com';Port=587;SocketOptions='StartTls';Username=$address;Password=$password;FromAddress=$address;FromName='OrçaFácil';ReplyTo=$address;TimeoutSeconds=20}) -Force
  $config | Add-Member NoteProperty Application ([pscustomobject]@{PublicBaseUrl='https://localhost:49900'}) -Force
  $config | Add-Member NoteProperty Security ([pscustomobject]@{PasswordResetPepper=(New-Pepper);SecurityEventPepper=(New-Pepper)}) -Force
  $config | Add-Member NoteProperty PasswordReset ([pscustomobject]@{TokenLifetimeMinutes=30;MinimumRequestIntervalSeconds=60;MaximumRequestsPerHour=3}) -Force
  $json = $config | ConvertTo-Json -Depth 12; $null = $json | ConvertFrom-Json; [IO.File]::WriteAllText($target,$json,[Text.UTF8Encoding]::new($false))
  try { $message = [Net.Mail.MailMessage]::new($address,'comercial@mnsoft.com.br','Teste de configuração OrçaFácil','Configuração SMTP validada.'); $smtp=[Net.Mail.SmtpClient]::new('smtp.gmail.com',587);$smtp.EnableSsl=$true;$smtp.Credentials=[Net.NetworkCredential]::new($address,$password);$smtp.Timeout=20000;$smtp.Send($message);Write-Host 'Configuração e teste SMTP concluídos com sucesso.' } catch [Net.Mail.SmtpException] { Write-Host 'Falha SMTP: autenticação, TLS, rede ou limite do provedor.' }
} finally { if($pointer -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)}; $password=$null;$securePassword=$null;if($rng){$rng.Dispose()} }
Write-Host 'Reinicie a aplicação para carregar a configuração.'
