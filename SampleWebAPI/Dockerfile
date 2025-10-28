# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy csproj and restore dependencies
COPY ["*.csproj", "./"]
RUN dotnet restore
# Copy everything else and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish image
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Fixed entrypoint - specify the exact DLL name
ENTRYPOINT ["dotnet", "SampleWebAPI.dll"]