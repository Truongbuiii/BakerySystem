@echo off
title Bakery Mobile App
echo ============================================================
echo   DANG KHOI DONG BAKERY MOBILE APP TREN WINDOWS DESKTOP...
echo ============================================================
set "APP_EXE=%~dp0src\5.Presentation\BakerySystem.MobileApp\bin\Debug\net10.0-windows10.0.19041.0\win-x64\BakerySystem.MobileApp.exe"

echo Dang kiem tra va dong bo ban build moi nhat...
dotnet build "%~dp0src\5.Presentation\BakerySystem.MobileApp\BakerySystem.MobileApp.csproj" -f net10.0-windows10.0.19041.0 --no-restore -v q

if exist "%APP_EXE%" (
    echo Dang khoi dong Bakery Mobile App...
    start "" "%APP_EXE%"
) else (
    echo [LOI] Khong the build hoac tim thay file BakerySystem.MobileApp.exe. Vui long kiem tra lai SDK .NET MAUI.
    pause
)
