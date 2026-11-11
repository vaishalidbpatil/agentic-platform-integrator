# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Portfolio.TradeOrderService.Models/Portfolio.TradeOrderService.Models.csproj src/Portfolio.TradeOrderService.Models/
COPY src/Portfolio.TradeOrderService.Configuration/Portfolio.TradeOrderService.Configuration.csproj src/Portfolio.TradeOrderService.Configuration/
COPY src/Portfolio.TradeOrderService.Database/Portfolio.TradeOrderService.Database.csproj src/Portfolio.TradeOrderService.Database/
COPY src/Portfolio.TradeOrderService.DataAccess/Portfolio.TradeOrderService.DataAccess.csproj src/Portfolio.TradeOrderService.DataAccess/
COPY src/Portfolio.TradeOrderService.Sync/Portfolio.TradeOrderService.Sync.csproj src/Portfolio.TradeOrderService.Sync/
RUN dotnet restore src/Portfolio.TradeOrderService.Sync/Portfolio.TradeOrderService.Sync.csproj

COPY src/ src/
RUN dotnet publish src/Portfolio.TradeOrderService.Sync/Portfolio.TradeOrderService.Sync.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Portfolio.TradeOrderService.Sync.dll"]
