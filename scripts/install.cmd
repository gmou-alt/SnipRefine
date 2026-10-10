@echo off
setlocal
rem ---------------------------------------------------------------------
rem  SnipRefine - install for the current user (no admin rights needed)
rem
rem    1) build dist\SnipRefine.exe
rem    2) register auto-start on logon
rem    3) create a desktop shortcut
rem    4) launch it
rem
rem  Usage:  scripts\install.cmd
rem ---------------------------------------------------------------------
for %%I in ("%~dp0..") do set "ROOT=%%~fI"

call "%~dp0build.cmd"
if errorlevel 1 exit /b 1

echo.
echo Registering auto-start ...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v SnapGrab /t REG_SZ /d "\"%ROOT%\dist\SnipRefine.exe\"" /f >nul
if errorlevel 1 (
  echo [WARN] Could not write the auto-start registry value.
  echo        You can still switch it on later inside the app window.
) else (
  echo [OK] Auto-start enabled.
)

echo Creating desktop shortcut ...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$w=New-Object -ComObject WScript.Shell; $lnk=Join-Path ([Environment]::GetFolderPath('Desktop')) 'SnipRefine.lnk'; $s=$w.CreateShortcut($lnk); $s.TargetPath='%ROOT%\dist\SnipRefine.exe'; $s.WorkingDirectory='%ROOT%\dist'; $s.IconLocation='%ROOT%\dist\SnipRefine.exe,0'; $s.Description='SnipRefine screenshot tool'; $s.Save(); Write-Host ('[OK] Shortcut: ' + $lnk)"

echo Starting SnipRefine ...
start "" "%ROOT%\dist\SnipRefine.exe"
exit /b 0
