FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# --- Cloud Run & Memory Leak Protection ---
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_GCHeapHardLimit=1800m
ENV COMPlus_gcServer=1
ENV COMPlus_GCLatencyLevel=1

# --- Diagnostic tools for memory leak investigation inside Cloud Run ---
RUN apt-get update && apt-get install -y procps
RUN dotnet tool install --global dotnet-counters
RUN dotnet tool install --global dotnet-gcdump
RUN dotnet tool install --global dotnet-dump
ENV PATH="$PATH:/root/.dotnet/tools"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["IBSWeb/IBSWeb.csproj", "IBSWeb/"]
COPY ["IBS.DataAccess/IBS.DataAccess.csproj", "IBS.DataAccess/"]
COPY ["IBS.DTOs/IBS.DTOs.csproj", "IBS.DTOs/"]
COPY ["IBS.Models/IBS.Models.csproj", "IBS.Models/"]
COPY ["IBS.Utility/IBS.Utility.csproj", "IBS.Utility/"]
COPY ["IBS.Services/IBS.Services.csproj", "IBS.Services/"]

RUN dotnet restore "./IBSWeb/IBSWeb.csproj"
COPY . .
WORKDIR "/src/IBSWeb"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IBSWeb.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER app

ENTRYPOINT ["dotnet", "IBSWeb.dll"]
