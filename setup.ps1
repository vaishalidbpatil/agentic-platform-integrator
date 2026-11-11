Write-Host "=== Trade Order Service Setup ===" -ForegroundColor Cyan

# Step 1 - Tear down everything: containers, orphans, and volumes
Write-Host "`nTearing down containers and volumes..." -ForegroundColor Yellow
docker compose down --remove-orphans -v
if ($LASTEXITCODE -ne 0) {
    Write-Host "docker compose down failed." -ForegroundColor Red
    exit 1
}

# Belt-and-suspenders: force-remove the volume if it still exists
$volumeName = "agentic-platform-integrator_postgres_data"
$volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Quiet $volumeName
if ($volumeExists) {
    Write-Host "Force-removing leftover volume: $volumeName" -ForegroundColor Yellow
    # Find and kill any container still holding the volume
    $holdingContainers = docker ps -aq --filter "volume=$volumeName"
    if ($holdingContainers) {
        docker rm -f $holdingContainers | Out-Null
    }
    docker volume rm $volumeName | Out-Null
}

# Step 2 - Start Postgres first and wait for it to be healthy
Write-Host "`nStarting Postgres..." -ForegroundColor Yellow
docker compose up -d postgres
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start Postgres." -ForegroundColor Red
    exit 1
}

Write-Host "Waiting for Postgres to be ready..."
$retries = 30
do {
    Start-Sleep -Seconds 1
    docker exec portfolio-postgres pg_isready -U admin -d tradeorders 2>&1 | Out-Null
    $retries--
} while ($LASTEXITCODE -ne 0 -and $retries -gt 0)

if ($LASTEXITCODE -ne 0) {
    Write-Host "Postgres did not become ready in time." -ForegroundColor Red
    docker compose logs postgres
    exit 1
}
Write-Host "Postgres is ready." -ForegroundColor Green

# Step 3 - Build and start the API (MigrationRunner runs on startup)
Write-Host "`nBuilding and starting API..." -ForegroundColor Yellow
docker compose up -d --build api
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start API." -ForegroundColor Red
    docker compose logs api
    exit 1
}

# Step 4 - Wait and show API startup logs
Write-Host "`nWaiting for API to start (10s)..." -ForegroundColor Yellow
Start-Sleep -Seconds 10
docker compose logs api --tail 30

Write-Host "`nSetup complete." -ForegroundColor Cyan
Write-Host "  Swagger UI : http://localhost:8080/swagger" -ForegroundColor Green
Write-Host "  Health     : http://localhost:8080/health/live" -ForegroundColor Green
