using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using MyRide.Application.Ports;
using MyRide.Application.Recovery;
using MyRide.Application.Sagas;
using MyRide.Infrastructure.Clients.Adapters;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Persistence;
using Polly;
using Refit;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Downstream service base URLs
var ridesApiUrl = builder.Configuration["DownstreamServices:RidesApi"]!;
var paymentsApiUrl = builder.Configuration["DownstreamServices:PaymentsApi"]!;
var payoutsApiUrl = builder.Configuration["DownstreamServices:PayoutsApi"]!;
var driversApiUrl = builder.Configuration["DownstreamServices:DriversApi"]!;

// Refit clients with resilience pipeline
builder.Services
    .AddRefitClient<IRidesApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(ridesApiUrl))
    .AddResilienceHandler("rides-resilience", ConfigureResilience);

builder.Services
    .AddRefitClient<IPaymentsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(paymentsApiUrl))
    .AddResilienceHandler("payments-resilience", ConfigureResilience);

builder.Services
    .AddRefitClient<IPayoutsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(payoutsApiUrl))
    .AddResilienceHandler("payouts-resilience", ConfigureResilience);

builder.Services
    .AddRefitClient<IDriversApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(driversApiUrl))
    .AddResilienceHandler("drivers-resilience", ConfigureResilience);

// SQL Server (SAGA persistence)
var connectionString = builder.Configuration.GetConnectionString("OrchestratorDb")
    ?? throw new InvalidOperationException("OrchestratorDb connection string is missing.");

builder.Services.AddDbContext<OrchestratorDbContext>(options =>
    options.UseSqlServer(connectionString));

// Downstream clients (adapters)
builder.Services.AddScoped<IDownstreamDriversClient, DriversApiClient>();
builder.Services.AddScoped<IDownstreamRidesClient, RidesApiClient>();
builder.Services.AddScoped<IDownstreamPaymentsClient, PaymentsApiClient>();
builder.Services.AddScoped<IDownstreamPayoutsClient, PayoutsApiClient>();

// SAGA repositories
builder.Services.AddScoped<IRequestRideSagaRepository, SqlRequestRideSagaRepository>();
builder.Services.AddScoped<ICompleteRideSagaRepository, SqlCompleteRideSagaRepository>();

// SAGAs
builder.Services.AddScoped<IRequestRideSaga, RequestRideSaga>();
builder.Services.AddScoped<ICompleteRideSaga, CompleteRideSaga>();

// Recovery job
builder.Services.AddHostedService<SagaRecoveryJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> pipeline)
{
    pipeline.AddTimeout(TimeSpan.FromSeconds(10));

    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });

    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        SamplingDuration = TimeSpan.FromSeconds(30),
        FailureRatio = 0.5,
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(5)
    });
}
