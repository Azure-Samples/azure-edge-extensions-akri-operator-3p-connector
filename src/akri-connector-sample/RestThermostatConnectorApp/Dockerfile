FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /app
COPY ["src/akri-connector-sample/RestThermostatConnectorApp/RestThermostatConnectorApp.csproj", "akri-connector-sample/RestThermostatConnectorApp/"]
COPY ["src/akri-connector-sample/RestThermostatConnectorApp/NuGet.config", "akri-connector-sample/RestThermostatConnectorApp/"]
RUN dotnet restore "akri-connector-sample/RestThermostatConnectorApp/RestThermostatConnectorApp.csproj" --configfile ./akri-connector-sample/RestThermostatConnectorApp/NuGet.config
COPY src ./src
COPY lib ./lib
WORKDIR "/app/src/akri-connector-sample/RestThermostatConnectorApp"
RUN dotnet build "RestThermostatConnectorApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RestThermostatConnectorApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestThermostatConnectorApp.dll"]
