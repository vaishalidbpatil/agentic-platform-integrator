# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Portfolio.TradeOrderService.Auth/Portfolio.TradeOrderService.Auth.csproj src/Portfolio.TradeOrderService.Auth/
RUN dotnet restore src/Portfolio.TradeOrderService.Auth/Portfolio.TradeOrderService.Auth.csproj

COPY src/Portfolio.TradeOrderService.Auth/ src/Portfolio.TradeOrderService.Auth/
RUN dotnet publish src/Portfolio.TradeOrderService.Auth/Portfolio.TradeOrderService.Auth.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001

ENTRYPOINT ["dotnet", "Portfolio.TradeOrderService.Auth.dll"]
