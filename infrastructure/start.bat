@echo off
cd /d "%~dp0"

echo.
echo  Stoixima Platform — startup
echo  ===========================
echo  Feed UI:   http://localhost:3000
echo  Admin UI:  http://localhost:3000/admin
echo  API:       http://localhost:5000
echo  Swagger:   http://localhost:5000/swagger
echo  Kafka UI:  http://localhost:8080
echo  Grafana:   http://localhost:3001  ^(admin / admin^)
echo.
echo  Logs are also saved to: infrastructure\docker.log
echo.

if not exist ".env" (
    echo  No .env found — copying .env.example.
    echo  Edit infrastructure\.env before adding real API keys.
    echo.
    copy .env.example .env >nul
)

echo  Checking Docker is running...
docker info >nul 2>&1
if errorlevel 1 (
    echo.
    echo  ERROR: Docker is not running!
    echo  Please start Docker Desktop first, then run this script again.
    echo.
    pause
    exit /b 1
)

echo  Docker OK.
echo.
echo  Building images and starting all services...
echo  Press Ctrl+C to stop.
echo.

docker compose up --build 2>&1 | tee docker.log

echo.
if errorlevel 1 (
    echo  ============================================================
    echo  docker compose exited with an error.
    echo  Scroll up to see what failed, or check docker.log
    echo  ============================================================
) else (
    echo  docker compose stopped cleanly.
)
echo.
pause
