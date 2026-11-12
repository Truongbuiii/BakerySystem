@echo off
title Bakery WebShop
echo ============================================================
echo   DANG KHOI DONG BAKERY WEBSHOP (BLAZOR SERVER)...
echo   Dia chi truy cap: http://localhost:5150/
echo ============================================================
dotnet run --project "%~dp0src\5.Presentation\BakerySystem.WebShop\BakerySystem.WebShop.csproj" --launch-profile http
pause
