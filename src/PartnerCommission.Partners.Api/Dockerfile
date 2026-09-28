# Сборка из корня репозитория:
#   docker build -f src/PartnerCommission.Partners.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# сначала только csproj + restore: слой с пакетами кэшируется и не пересобирается при правке кода
COPY src/PartnerCommission.Shared/PartnerCommission.Shared.csproj src/PartnerCommission.Shared/
COPY src/PartnerCommission.Contracts/PartnerCommission.Contracts.csproj src/PartnerCommission.Contracts/
COPY src/PartnerCommission.Partners.Api/PartnerCommission.Partners.Api.csproj src/PartnerCommission.Partners.Api/
RUN dotnet restore src/PartnerCommission.Partners.Api/PartnerCommission.Partners.Api.csproj

COPY src/PartnerCommission.Shared/ src/PartnerCommission.Shared/
COPY src/PartnerCommission.Contracts/ src/PartnerCommission.Contracts/
COPY src/PartnerCommission.Partners.Api/ src/PartnerCommission.Partners.Api/
RUN dotnet publish src/PartnerCommission.Partners.Api/PartnerCommission.Partners.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# curl — для healthcheck контейнера (в образе aspnet его нет)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# встроенный в образы .NET 8 непривилегированный пользователь; порт по умолчанию — 8080
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "PartnerCommission.Partners.Api.dll"]
