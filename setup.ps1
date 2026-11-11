Write-Host "=== Trade Order Service Setup ===" -ForegroundColor Cyan

# Step 1 - Tear down existing containers and volumes for a clean slate
Write-Host "`nTearing down existing containers and volumes..." -ForegroundColor Yellow
docker compose down -v
if ($LASTEXITCODE -ne 0) {
    Write-Host "docker compose down failed." -ForegroundColor Red
    exit 1
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

# Step 4 - Tail API logs briefly to confirm startup and migrations
Write-Host "`nAPI startup logs (10s):" -ForegroundColor Yellow
$timeout = 10
$elapsed = 0
while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++
}
docker compose logs api --tail 30

Write-Host "`nSetup complete." -ForegroundColor Cyan
Write-Host "  Swagger UI : http://localhost:8080/swagger" -ForegroundColor Green
Write-Host "  Health     : http://localhost:8080/health/live" -ForegroundColor Green
