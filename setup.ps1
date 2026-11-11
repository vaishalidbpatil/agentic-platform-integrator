Write-Host "=== Trade Order Service Setup ===" -ForegroundColor Cyan

# Step 1 - Start containers
Write-Host "`nStarting Docker containers..."
docker compose up -d

# Step 2 - Wait for Postgres to be healthy
Write-Host "Waiting for Postgres to be ready..."
do {
    Start-Sleep -Seconds 1
    $ready = docker exec portfolio-postgres pg_isready -U admin -d tradeorders 2>$null
} while ($LASTEXITCODE -ne 0)
Write-Host "Postgres is ready." -ForegroundColor Green

# Step 3 - Run migrations
Write-Host "`nRunning migrations..."
Get-ChildItem -Path "db\migrations\*.sql" | Sort-Object Name | ForEach-Object {
    Get-Content $_.FullName | docker exec -i portfolio-postgres psql -U admin -d tradeorders
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Applied: $($_.Name)" -ForegroundColor Green
    } else {
        Write-Host "  FAILED: $($_.Name)" -ForegroundColor Red
        exit 1
    }
}

# Step 4 - Verify tables
Write-Host "`nVerifying tables..."
docker exec -it portfolio-postgres psql -U admin -d tradeorders -c "\dt"

Write-Host "`nSetup complete." -ForegroundColor Cyan
