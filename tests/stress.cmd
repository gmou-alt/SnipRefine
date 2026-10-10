@echo off
call "%~dp0run.cmd" --stress
set "RESULT=%ERRORLEVEL%"
echo.
pause
exit /b %RESULT%
