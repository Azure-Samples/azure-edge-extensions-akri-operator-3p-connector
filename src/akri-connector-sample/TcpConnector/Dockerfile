FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["akri-connector-sample/BmwTcpAdapter/BmwTcpAdapter.csproj", "akri-connector-sample/BmwTcpAdapter/"]
COPY ["akri-connector-sample/BmwTcpAdapter/NuGet.config", "akri-connector-sample/BmwTcpAdapter/"]
RUN dotnet restore "akri-connector-sample/BmwTcpAdapter/BmwTcpAdapter.csproj" --configfile ./akri-connector-sample/BmwTcpAdapter/NuGet.config
COPY . .
WORKDIR "/src/akri-connector-sample/BmwTcpAdapter"
RUN dotnet build "BmwTcpAdapter.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BmwTcpAdapter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BmwTcpAdapter.dll"]
