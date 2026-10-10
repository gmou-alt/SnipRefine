@echo off
setlocal
rem ---------------------------------------------------------------------
rem  SnipRefine - pack a release zip
rem
rem  Builds the exe and produces dist\SnipRefine-<version>-win.zip, which is
rem  what gets uploaded to a GitHub Release.
rem
rem  Usage:  scripts\pack.cmd
rem ---------------------------------------------------------------------
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "VERSION=1.22.1"

call "%~dp0build.cmd"
if errorlevel 1 exit /b 1

if defined SNIPREFINE_SIGN_CERT_THUMBPRINT (
  if not defined SNIPREFINE_TIMESTAMP_SERVER (
    echo [ERROR] Set SNIPREFINE_TIMESTAMP_SERVER before signing.
    exit /b 1
  )
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0sign.ps1" -CertificateThumbprint "%SNIPREFINE_SIGN_CERT_THUMBPRINT%" -TimestampServer "%SNIPREFINE_TIMESTAMP_SERVER%"
  if errorlevel 1 exit /b 1
) else (
  echo [INFO] Packing an unsigned executable.
)

set "STAGE=%ROOT%\dist\SnipRefine-%VERSION%"
set "ZIP=%ROOT%\dist\SnipRefine-%VERSION%-win.zip"

if exist "%STAGE%" rmdir /s /q "%STAGE%"
if exist "%ZIP%" del /q "%ZIP%"
mkdir "%STAGE%"

copy /y "%ROOT%\dist\SnipRefine.exe" "%STAGE%\" >nul
copy /y "%ROOT%\dist\SnipRefine.exe.config" "%STAGE%\" >nul
copy /y "%ROOT%\README.md" "%STAGE%\" >nul
copy /y "%ROOT%\README.en.md" "%STAGE%\" >nul
copy /y "%ROOT%\LICENSE" "%STAGE%\" >nul
copy /y "%ROOT%\CONTRIBUTING.md" "%STAGE%\" >nul
mkdir "%STAGE%\docs"
rem Ship only user-facing docs in the release zip. Internal docs and fixtures stay in the repo.
copy /y "%ROOT%\docs\USER-GUIDE.md"    "%STAGE%\docs\" >nul
copy /y "%ROOT%\docs\IMAGE-EDITING.md" "%STAGE%\docs\" >nul
copy /y "%ROOT%\docs\AI-REGION.md"     "%STAGE%\docs\" >nul
copy /y "%ROOT%\docs\RELEASE-NOTES.md" "%STAGE%\docs\" >nul
mkdir "%STAGE%\internal-skills\image-edit"
copy /y "%ROOT%\internal-skills\image-edit\SKILL.md" "%STAGE%\internal-skills\image-edit\" >nul

echo Packing %ZIP% ...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%STAGE%' -DestinationPath '%ZIP%' -Force"
if not exist "%ZIP%" (
  echo [WARN] Compress-Archive failed, trying tar.exe ...
  tar.exe -a -c -f "%ZIP%" -C "%ROOT%\dist" "SnipRefine-%VERSION%"
)

rmdir /s /q "%STAGE%"

if not exist "%ZIP%" (
  echo [ERROR] Could not create the release zip.
  if "%CI%"=="" pause
  exit /b 1
)

for %%I in ("%ZIP%") do echo [OK] %%~fI  ^(%%~zI bytes^)
if "%CI%"=="" pause
exit /b 0
