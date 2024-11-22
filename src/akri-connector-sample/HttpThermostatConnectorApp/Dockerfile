FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["akri-connector-sample/HttpThermostatConnectorApp/HttpThermostatConnectorApp.csproj", "akri-connector-sample/HttpThermostatConnectorApp/"]
COPY ["akri-connector-sample/HttpThermostatConnectorApp/NuGet.config", "akri-connector-sample/HttpThermostatConnectorApp/"]
RUN dotnet restore "akri-connector-sample/HttpThermostatConnectorApp/HttpThermostatConnectorApp.csproj" --configfile ./akri-connector-sample/HttpThermostatConnectorApp/NuGet.config
COPY . .
WORKDIR "/src/akri-connector-sample/HttpThermostatConnectorApp"
RUN dotnet build "HttpThermostatConnectorApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HttpThermostatConnectorApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HttpThermostatConnectorApp.dll"]
