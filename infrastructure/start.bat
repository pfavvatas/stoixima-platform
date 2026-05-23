@echo off
cd /d "%~dp0"

echo.
echo  Stoixima Platform
echo  =================
echo  Feed UI:  http://localhost:3000
echo  Admin UI: http://localhost:3000/admin
echo  API:      http://localhost:5000
echo.

if not exist ".env" (
    copy .env.example .env >nul
    echo  Copied .env.example to .env
)

echo  Checking Docker...
docker info >nul 2>&1
if errorlevel 1 (
    echo.
    echo  ERROR: Docker Desktop is not running.
    echo  Start Docker Desktop first, then run this file again.
    echo.
    pause
    exit /b 1
)

echo  Docker OK. Starting services ^(this takes a few minutes on first run^)...
echo  Press Ctrl+C to stop.
echo.

docker compose up --build

echo.
echo  Stopped. Exit code: %errorlevel%
echo.
pause
