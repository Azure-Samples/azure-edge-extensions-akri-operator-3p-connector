FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["akri-connector-sample/TcpConnector/TcpConnector.csproj", "akri-connector-sample/TcpConnector/"]
COPY ["akri-connector-sample/TcpConnector/NuGet.config", "akri-connector-sample/TcpConnector/"]
RUN dotnet restore "akri-connector-sample/TcpConnector/TcpConnector.csproj" --configfile ./akri-connector-sample/TcpConnector/NuGet.config
COPY . .
WORKDIR "/src/akri-connector-sample/TcpConnector"
RUN dotnet build "TcpConnector.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TcpConnector.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TcpConnector.dll"]
