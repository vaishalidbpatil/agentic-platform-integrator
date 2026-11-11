# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY src/Portfolio.TradeOrderService.Models/Portfolio.TradeOrderService.Models.csproj src/Portfolio.TradeOrderService.Models/
COPY src/Portfolio.TradeOrderService.Configuration/Portfolio.TradeOrderService.Configuration.csproj src/Portfolio.TradeOrderService.Configuration/
COPY src/Portfolio.TradeOrderService.Database/Portfolio.TradeOrderService.Database.csproj src/Portfolio.TradeOrderService.Database/
COPY src/Portfolio.TradeOrderService.DataAccess/Portfolio.TradeOrderService.DataAccess.csproj src/Portfolio.TradeOrderService.DataAccess/
COPY src/Portfolio.TradeOrderService.ServiceInterfaces/Portfolio.TradeOrderService.ServiceInterfaces.csproj src/Portfolio.TradeOrderService.ServiceInterfaces/
COPY src/Portfolio.TradeOrderService.Services/Portfolio.TradeOrderService.Services.csproj src/Portfolio.TradeOrderService.Services/
COPY src/Portfolio.TradeOrderService.Api/Portfolio.TradeOrderService.Api.csproj src/Portfolio.TradeOrderService.Api/
RUN dotnet restore src/Portfolio.TradeOrderService.Api/Portfolio.TradeOrderService.Api.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/Portfolio.TradeOrderService.Api/Portfolio.TradeOrderService.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Portfolio.TradeOrderService.Api.dll"]
