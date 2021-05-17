FROM microsoft/aspnetcore-build:2.0
ARG BUILD_CONFIGURATION=Debug
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 80

WORKDIR /src
COPY ["IoTHubGateway.Server/IoTHubGateway.Server.csproj", "IoTHubGateway.Server/"]

RUN dotnet restore "IoTHubGateway.Server/IoTHubGateway.Server.csproj"
COPY . .
WORKDIR "/src/IoTHubGateway.Server"
RUN dotnet build --no-restore "IoTHubGateway.Server.csproj" -c $BUILD_CONFIGURATION

RUN echo "exec dotnet run --no-build --no-launch-profile -c $BUILD_CONFIGURATION --" > /entrypoint.sh

ENTRYPOINT ["/bin/bash", "/entrypoint.sh"]