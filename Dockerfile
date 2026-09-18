FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/LPUPlus.Server/*.csproj ./src/LPUPlus.Server/
COPY src/LPUPlus.Protocol/*.csproj ./src/LPUPlus.Protocol/
RUN dotnet restore src/LPUPlus.Server/LPUPlus.Server.csproj

# Copy everything else and build
COPY src/LPUPlus.Server/ ./src/LPUPlus.Server/
COPY src/LPUPlus.Protocol/ ./src/LPUPlus.Protocol/
WORKDIR /app/src/LPUPlus.Server
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/src/LPUPlus.Server/out .

# Render dynamically assigns the PORT environment variable
ENV ASPNETCORE_URLS=http://*:${PORT:-5000}

ENTRYPOINT ["dotnet", "LPUPlus.Server.dll"]
