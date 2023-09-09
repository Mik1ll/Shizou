#Requires -RunAsAdministrator

$cert = New-SelfSignedCertificate -DnsName @("shizou.home", "*.shizou.home", "localhost") -CertStoreLocation "Cert:\CurrentUser\My"
$certsPath = (Read-Host "Where to save cert files").Trim('"', "'")
if (-not (Test-Path $certsPath) -or (Get-Item $certsPath) -isnot [System.IO.DirectoryInfo]) {
    Write-Output "`"$certsPath`" is not a valid directory, aborting"
    Exit
}
$password = ConvertTo-SecureString (Read-Host "Enter a certificate password") -AsPlainText -Force
$certPath = Join-Path $certsPath "shizou.home.cer"
$cert | Export-Certificate -FilePath $certPath | Out-Null
$certKeyPath = Join-Path $certsPath "shizou.home.pfx"
$cert | Export-PfxCertificate -FilePath $certKeyPath -Password $password | Out-Null
$null = Import-PfxCertificate -FilePath $certKeyPath -CertStoreLocation 'Cert:\CurrentUser\Root' -Password $password