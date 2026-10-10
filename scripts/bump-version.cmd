@echo off
setlocal
rem ---------------------------------------------------------------------
rem  bump-version.cmd - update all version numbers (source version + package name)
rem
rem  Usage:  scripts\bump-version.cmd 1.22.1
rem ---------------------------------------------------------------------
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
if "%~1"=="" (
  echo Usage: scripts\bump-version.cmd ^<x.y.z^>
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\tools\set-version.ps1" -Version "%~1"
exit /b %errorlevel%
