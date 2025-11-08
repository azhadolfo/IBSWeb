# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for better layer caching
COPY ["IBSWeb/IBSWeb.csproj", "IBSWeb/"]
COPY ["IBS.DataAccess/IBS.DataAccess.csproj", "IBS.DataAccess/"]
COPY ["IBS.DTOs/IBS.DTOs.csproj", "IBS.DTOs/"]
COPY ["IBS.Models/IBS.Models.csproj", "IBS.Models/"]
COPY ["IBS.Utility/IBS.Utility.csproj", "IBS.Utility/"]
COPY ["IBS.Services/IBS.Services.csproj", "IBS.Services/"]

# Restore dependencies (cached unless .csproj files change)
RUN dotnet restore "./IBSWeb/IBSWeb.csproj"

# Copy source code after restore
COPY . .

WORKDIR "/src/IBSWeb"

# Combine build and publish in one stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IBSWeb.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IBSWeb.dll"]