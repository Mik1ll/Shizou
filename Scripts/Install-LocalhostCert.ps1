$ErrorActionPreference = 'Stop'
$cert = New-SelfSignedCertificate -Subject 'Shizou' -TextExtension '2.5.29.17={text}DNS=localhost&IPAddress=127.0.0.1&IPAddress=::1' -CertStoreLocation 'Cert:\CurrentUser\My'
$certsPath = (Read-Host "Where to save cert files").Trim('"', "'")
New-Item -Type Directory -Path $certsPath -ErrorAction SilentlyContinue
$password = ConvertTo-SecureString (Read-Host "Enter a certificate password") -AsPlainText -Force
$certPath = Join-Path $certsPath "shizou.pfx"
Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $password
Import-PfxCertificate -FilePath $certPath -CertStoreLocation 'Cert:\CurrentUser\Root' -Password $password
