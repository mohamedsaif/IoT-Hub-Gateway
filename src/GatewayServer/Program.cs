using GatewayServer.Controllers;
using GatewayServer.Models;
using GatewayServer.Repositories;
using GatewayServer.Utils;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddApplicationInsightsKubernetesEnricher();

// Dapr init
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3600";
var daprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "60000";
builder.Services.AddDaprClient(builder => builder
    .UseHttpEndpoint($"http://localhost:{daprHttpPort}")
    .UseGrpcEndpoint($"http://localhost:{daprGrpcPort}"));

///TODO: Finalize implementation
// Distributed Cache
//builder.Services.AddSingleton<IDistributedCache, DaprDeviceCacheRepository>();

// Gateway Server
var runnerConfigs = RunnerConfiguration.Load(builder.Configuration);
builder.Services.AddSingleton<RunnerConfiguration>(runnerConfigs);

var runnerStats = new RunnerStats();
builder.Services.AddSingleton<RunnerStats>(runnerStats);

builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<ServiceHealthCheck>("default", args: new object[] { runnerConfigs });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/healthz");

app.MapHealthChecks("/health-details", new HealthCheckOptions
{
    ResponseWriter = ServiceHealthCheck.WriteResponse
});

app.UseAuthorization();

app.MapControllers();

app.Run();
