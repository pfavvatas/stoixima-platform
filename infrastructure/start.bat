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

if not exist ".env" (
    echo  No .env found — copying .env.example.
    echo  Edit infrastructure\.env before adding real API keys.
    echo.
    copy .env.example .env >nul
)

echo  Building images and starting all services...
echo  Press Ctrl+C to stop.
echo.

docker compose up --build
