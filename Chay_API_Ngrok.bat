@echo off
title Bakery API + ngrok
echo ============================================================
echo   DANG KHOI DONG BAKERY API VA NGROK...
echo ============================================================

set "ROOT=%~dp0"
set "API_PROJECT=%ROOT%src\4.API\BakerySystem.API\BakerySystem.API.csproj"

where ngrok >nul 2>&1
if errorlevel 1 (
    echo [LOI] Khong tim thay ngrok trong PATH.
    echo Hay mo lai cua so CMD sau khi cai ngrok hoac cai lai ngrok.
    pause
    exit /b 1
)

echo Dang build API...
dotnet build "%API_PROJECT%" --no-restore -v q
if errorlevel 1 (
    echo [LOI] Build API that bai.
    pause
    exit /b 1
)

echo Dang mo API tai http://localhost:5004...
start "Bakery API" cmd /k "dotnet run --project ""%API_PROJECT%"" --launch-profile http --no-build"

timeout /t 3 /nobreak >nul

echo.
echo Tunnel ngrok se hien thi URL HTTPS ben duoi.
echo Khong dong cua so nay khi dien thoai dang su dung ung dung.
echo.
ngrok http 5004

echo.
echo [THONG BAO] Tunnel ngrok da dung.
pause
