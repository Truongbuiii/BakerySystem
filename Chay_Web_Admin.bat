@echo off
title Bakery WebAdmin
echo ============================================================
echo   DANG KHOI DONG BAKERY WEBDMIN (BLAZOR SERVER)...
echo   Dia chi truy cap: http://localhost:5149/
echo ============================================================
dotnet run --project "%~dp0src\5.Presentation\BakerySystem.WebAdmin\BakerySystem.WebAdmin.csproj" --launch-profile http
pause
