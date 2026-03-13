using Asp.Versioning;
using Microsoft.Extensions.Http.Resilience;
using MyRide.API.Clients;
using Polly;
using Refit;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
    // 1. Overall timeout — cancel if entire operation exceeds 10s
    pipeline.AddTimeout(TimeSpan.FromSeconds(10));

    // 2. Retry — 3 attempts with exponential backoff + jitter
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });

    // 3. Circuit breaker — opens after 50% failure rate over 5+ calls in 30s
    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        SamplingDuration = TimeSpan.FromSeconds(30),
        FailureRatio = 0.5,
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(15)
    });
}
