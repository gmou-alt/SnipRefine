@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework compiler was not found. Install .NET Framework 4.5 or later.
  pause
  exit /b 1
)
if not exist "dist" mkdir "dist"
echo Building SnipRefine with the Windows .NET Framework compiler...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /langversion:5 /codepage:65001 /out:"dist\SnipRefine-rebuild.exe" /win32icon:"assets\app.ico" /win32manifest:"assets\app.manifest" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Security.dll /r:System.Web.Extensions.dll "src\*.cs"
if errorlevel 1 (
  echo Build failed. Keep this window open and capture the compiler error.
  pause
  exit /b 1
)
move /y "dist\SnipRefine-rebuild.exe" "dist\SnipRefine.exe" >nul
if errorlevel 1 (
  echo Close the running SnipRefine from the system tray, then run Start.cmd again.
  pause
  exit /b 1
)
copy /y "assets\SnipRefine.exe.config" "dist\SnipRefine.exe.config" >nul
if errorlevel 1 (
  echo Runtime configuration could not be copied. Check folder permissions.
  pause
  exit /b 1
)
echo Starting SnipRefine. This window stays open until the app exits.
"dist\SnipRefine.exe"
set "RUN_RESULT=%errorlevel%"
if not "%RUN_RESULT%"=="0" (
  echo SnipRefine exited with error code %RUN_RESULT%. Capture this window for diagnosis.
  pause
)
exit /b %RUN_RESULT%
