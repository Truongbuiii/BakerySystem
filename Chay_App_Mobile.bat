@echo off
title Bakery Mobile App
echo ============================================================
echo   DANG KHOI DONG BAKERY MOBILE APP TREN WINDOWS DESKTOP...
echo ============================================================
set EXE_PATH=%~dp0src\5.Presentation\BakerySystem.MobileApp\bin\Debug\net10.0-windows10.0.19041.0\win-x64\BakerySystem.MobileApp.exe
if not exist "%EXE_PATH%" (
    echo Dang bien dich Mobile App lan dau tien, vui long cho giay lat...
    dotnet build "%~dp0src\5.Presentation\BakerySystem.MobileApp\BakerySystem.MobileApp.csproj" -f net10.0-windows10.0.19041.0
)
start "" "%EXE_PATH%"
