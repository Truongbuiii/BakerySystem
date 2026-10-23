@echo off
title Bakery API
echo ============================================================
echo   DANG KHOI DONG BAKERY API...
echo ============================================================

set "API_PROJECT=%~dp0src\4.API\BakerySystem.API\BakerySystem.API.csproj"

echo Dang kiem tra va dong bo ban build moi nhat...
dotnet build "%API_PROJECT%" --no-restore -v q

if errorlevel 1 (
    echo [LOI] Build Bakery API that bai. Vui long kiem tra SDK .NET va loi build.
    pause
    exit /b 1
)

echo.
echo API se chay tai:
echo   http://localhost:5004
echo   http://%COMPUTERNAME%:5004
echo.
echo Khong dong cua so nay trong khi MobileApp dang su dung API.
echo.

dotnet run --project "%API_PROJECT%" --launch-profile http --no-build

if errorlevel 1 (
    echo.
    echo [LOI] Bakery API da dung hoac khong the khoi dong.
    pause
)
