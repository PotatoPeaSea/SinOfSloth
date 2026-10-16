@echo off
rem One-click installer for LoL Guard. Double-click this file.
rem It asks for administrator permission, installs the LolGuard service, and shows its status.

if /i "%~1"=="--elevated" goto install

net session >nul 2>&1
if %errorlevel% equ 0 goto install

echo Requesting administrator permission...
powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '--elevated' -Verb RunAs"
if errorlevel 1 (
    echo.
    echo Administrator permission was not granted, so nothing was installed.
    pause
)
exit /b

:install
cd /d "%~dp0"
echo Installing LoL Guard...
"%~dp0LolGuard.exe" install
if errorlevel 1 (
    echo.
    echo Install failed. See the message above.
    pause
    exit /b 1
)

echo.
"%~dp0LolGuard.exe" status
echo.
echo LoL Guard is installed. It starts automatically with Windows.
pause
