FROM mcr.microsoft.com/dotnet/sdk:2.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY . .
RUN mkdir -p /home/site/wwwroot
RUN dotnet publish *.csproj --output /home/site/wwwroot

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:2.1
WORKDIR /home/site/wwwroot
COPY --from=build-env /home/site/wwwroot .
ENTRYPOINT ["dotnet", "IoTHubGateway.Server.dll"]