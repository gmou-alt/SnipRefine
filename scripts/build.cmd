@echo off
setlocal
rem ---------------------------------------------------------------------
rem  SnipRefine - build
rem
rem  Compiles src\*.cs into dist\SnipRefine.exe with the C# compiler that
rem  ships inside the .NET Framework (no SDK, no NuGet, no internet).
rem
rem  Usage:  scripts\build.cmd
rem ---------------------------------------------------------------------
for %%I in ("%~dp0..") do set "ROOT=%%~fI"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" goto :nocs

if not exist "%ROOT%\dist" mkdir "%ROOT%\dist"

set "BUILD_EXE=%ROOT%\dist\SnipRefine.build.exe"

echo Building dist\SnipRefine.exe ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /langversion:5 /codepage:65001 /out:"%BUILD_EXE%" /win32icon:"%ROOT%\assets\app.ico" /win32manifest:"%ROOT%\assets\app.manifest" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Security.dll /r:System.Web.Extensions.dll "%ROOT%\src\*.cs"
if errorlevel 1 goto :fail

move /y "%BUILD_EXE%" "%ROOT%\dist\SnipRefine.exe" >nul
if errorlevel 1 (
  echo [ERROR] Exit SnipRefine from the tray before replacing the executable.
  del /q "%BUILD_EXE%" >nul 2>&1
  exit /b 1
)

copy /y "%ROOT%\assets\SnipRefine.exe.config" "%ROOT%\dist\SnipRefine.exe.config" >nul
if errorlevel 1 goto :fail
echo [OK] %ROOT%\dist\SnipRefine.exe
exit /b 0

:nocs
echo [ERROR] csc.exe not found. Install .NET Framework 4.5+ ^(preinstalled on Windows 10/11^).
echo         You can still run the tool without building: scripts\SnipRefine.vbs
if "%CI%"=="" pause
exit /b 1

:fail
del /q "%BUILD_EXE%" >nul 2>&1
echo.
echo [ERROR] Build failed - see the compiler messages above.
if "%CI%"=="" pause
exit /b 1
