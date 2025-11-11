FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /app
COPY ["src/deprecated/TcpConnector/TcpConnector.csproj", "src/deprecated/TcpConnector/"]
COPY ["src/deprecated/TcpConnector/NuGet.config", "src/deprecated/TcpConnector/"]
RUN dotnet restore "src/deprecated/TcpConnector/TcpConnector.csproj" --configfile ./src/deprecated/TcpConnector/NuGet.config
COPY src ./src
COPY lib ./lib
WORKDIR "/app/src/deprecated/TcpConnector"
RUN dotnet build "TcpConnector.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish   
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TcpConnector.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TcpConnector.dll"]
