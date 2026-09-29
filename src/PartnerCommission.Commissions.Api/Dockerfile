FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PartnerCommission.Shared/PartnerCommission.Shared.csproj src/PartnerCommission.Shared/
COPY src/PartnerCommission.Contracts/PartnerCommission.Contracts.csproj src/PartnerCommission.Contracts/
COPY src/PartnerCommission.Commissions.Domain/PartnerCommission.Commissions.Domain.csproj src/PartnerCommission.Commissions.Domain/
COPY src/PartnerCommission.Commissions.Api/PartnerCommission.Commissions.Api.csproj src/PartnerCommission.Commissions.Api/
RUN dotnet restore src/PartnerCommission.Commissions.Api/PartnerCommission.Commissions.Api.csproj

COPY src/PartnerCommission.Shared/ src/PartnerCommission.Shared/
COPY src/PartnerCommission.Contracts/ src/PartnerCommission.Contracts/
COPY src/PartnerCommission.Commissions.Domain/ src/PartnerCommission.Commissions.Domain/
COPY src/PartnerCommission.Commissions.Api/ src/PartnerCommission.Commissions.Api/
RUN dotnet publish src/PartnerCommission.Commissions.Api/PartnerCommission.Commissions.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "PartnerCommission.Commissions.Api.dll"]
