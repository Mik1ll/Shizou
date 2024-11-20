$ErrorActionPreference = 'Stop'
$cert = New-SelfSignedCertificate -Subject 'Shizou' -TextExtension @('2.5.29.17={text}DNS=localhost&IPAddress=127.0.0.1&IPAddress=::1', '2.5.29.37={text}1.3.6.1.5.5.7.3.1') -CertStoreLocation 'Cert:\CurrentUser\My'
$password = ConvertTo-SecureString (Read-Host "Enter a certificate password") -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath "shizou.pfx" -Password $password
Import-PfxCertificate -FilePath "shizou.pfx" -CertStoreLocation 'Cert:\CurrentUser\Root' -Password $password
