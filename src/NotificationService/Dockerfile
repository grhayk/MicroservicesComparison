# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["NotificationService.csproj", "./"]
RUN dotnet restore "NotificationService.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "NotificationService.dll"]