@echo off
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" exit /b 1
if not exist "%ROOT%\dist" mkdir "%ROOT%\dist"
"%CSC%" /nologo /target:exe /langversion:5 /codepage:65001 /main:SnapGrab.RegressionTests /out:"%ROOT%\dist\SnipRefine.Tests.exe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Security.dll /r:System.Web.Extensions.dll "%ROOT%\src\*.cs" "%ROOT%\tests\*.cs"
if errorlevel 1 exit /b 1
"%ROOT%\dist\SnipRefine.Tests.exe" %*
exit /b %errorlevel%
