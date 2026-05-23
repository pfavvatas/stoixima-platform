# One-command startup for the Stoixima platform.
# Usage: .\start.ps1 [-Build] [-InfraOnly]
param(
    [switch]$Build,
    [switch]$InfraOnly
)

Set-Location $PSScriptRoot

$buildFlag = if ($Build) { "--build" } else { "" }

# Copy .env.example → .env if not present
if (-not (Test-Path ".env")) {
    Write-Host "No .env found — copying .env.example. Edit it before adding real API keys." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env"
}

Write-Host ""
Write-Host "Starting Stoixima platform..." -ForegroundColor Cyan
Write-Host "  Feed UI:   http://localhost:3000"
Write-Host "  Admin UI:  http://localhost:3000/admin"
Write-Host "  API:       http://localhost:5000"
Write-Host "  Swagger:   http://localhost:5000/swagger"
Write-Host "  Kafka UI:  http://localhost:8080"
Write-Host "  Grafana:   http://localhost:3001  (admin / admin)"
Write-Host ""

if ($InfraOnly) {
    $services = "zookeeper kafka kafka-ui kafka-init postgres clickhouse redis prometheus grafana"
    if ($buildFlag) {
        docker compose up -d --build $services.Split(" ")
    } else {
        docker compose up -d $services.Split(" ")
    }
} else {
    if ($buildFlag) {
        docker compose up -d --build
    } else {
        docker compose up -d
    }
}

Write-Host ""
Write-Host "All services started. Run 'docker compose logs -f' to follow logs." -ForegroundColor Green
