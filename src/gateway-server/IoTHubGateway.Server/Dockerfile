#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
COPY ["src/IoTHubGateway.Server/IoTHubGateway.Server.csproj", "IoTHubGateway.Server/"]
RUN dotnet restore "IoTHubGateway.Server/IoTHubGateway.Server.csproj"
COPY . .
WORKDIR "/src/IoTHubGateway.Server"
RUN dotnet build "IoTHubGateway.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IoTHubGateway.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IoTHubGateway.Server.dll"]