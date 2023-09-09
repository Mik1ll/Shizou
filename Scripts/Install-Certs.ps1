$cert = New-SelfSignedCertificate -DnsName @("shizou.home", "*.shizou.home") -CertStoreLocation "Cert:\LocalMachine\My"
$certsPath = Read-Host "Where to save cert files:"
if (!(Test-Path $certsPath) -or (Get-Item $certsPath) -isnot [System.IO.DirectoryInfo]) {
    Write-Output "Not a valid directory, aborting"
    Exit
}
$password = ConvertTo-SecureString (Read-Host "Enter a certificate password:") -AsPlainText -Force
$certPath = Join-Path $certsPath "shizou.home.cer"
$cert | Export-Certificate -FilePath $certPath
$certKeyPath = Join-Path $certsPath "shizou.home.pfx"
$cert | Export-PfxCertificate -FilePath $certKeyPath -Password $password
Import-PfxCertificate -FilePath $certKeyPath -CertStoreLocation 'Cert:\LocalMachine\Root' -Password $password