$mpvPath = Read-Host "Enter full path of mpv"
$mpvPath = $mpvPath.Trim('"')

$mpvRunPath = "$env:APPDATA\Shizou\mpvrun.bat"


$mpvKey = New-Item -Path 'HKCU:\SOFTWARE\Classes' -Name 'mpv' -Force
$mpvKey.SetValue('', 'URL:mpv Protocol')
$mpvKey.SetValue('URL Protocol', 'b')
$iconKey = $mpvKey.CreateSubKey('DefaultIcon')
$iconKey.SetValue('', "`"$mpvPath`",1")
$cmdKey = $mpvKey.CreateSubKey('shell\open\command')
$cmdKey.SetValue('', "`"$mpvRunPath`" `"%1`"")

$mpvRun = @"
@echo off
setlocal EnableDelayedExpansion

for /f "tokens=1,* delims=?" %%A in ("%~1") do (
  set url=%%A
  set url=!url:~4!
  set cookie=%%B
)

if /i "%url:~0,8%"=="https://" (
  start "" /B `"$mpvPath`" "--no-terminal" "--no-ytdl" "--http-header-fields=Cookie: %cookie%" "%url%"
)
"@

New-Item -ItemType Directory -Path "$env:APPDATA\Shizou" -Force
Set-Content -Path "$env:APPDATA\Shizou\mpvrun.bat" -Value $mpvRun
