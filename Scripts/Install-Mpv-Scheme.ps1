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

rem Replaces __ with equal signs
setlocal EnableDelayedExpansion
set arg=%~1
set arg=!arg:__==!

for /f "tokens=1,* delims=?" %%A in ("%arg%") do (
  set url=%%A
  set url=!url:~4!
  set cookie=%%B
)

rem Work around echo on not activating inside if block
set "echo_on=echo on&for %%. in (.) do"

if /i "%url:~0,8%"=="https://" (
  %echo_on% `"$mpvPath`" --no-ytdl "--http-header-fields=Cookie: %cookie%" %url%
)

"@

New-Item -ItemType Directory -Path "$env:APPDATA\Shizou" -Force
Set-Content -Path "$env:APPDATA\Shizou\mpvrun.bat" -Value $mpvRun
