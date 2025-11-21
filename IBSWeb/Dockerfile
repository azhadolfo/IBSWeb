FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

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
