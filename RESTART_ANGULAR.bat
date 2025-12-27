@echo off
echo Stopping Angular Dev Server...
taskkill /F /IM node.exe /T >nul 2>&1
timeout /t 2 /nobreak >nul

echo Starting Angular Dev Server...
cd src\AutoPartShop.WebApp
start cmd /k "npm start"

echo.
echo Angular dev server is restarting...
echo Wait 30 seconds, then open: http://localhost:4200/inventory/stock
echo.
pause
