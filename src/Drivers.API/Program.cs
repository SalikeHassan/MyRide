using System.Text.Json.Serialization;
using Asp.Versioning;
using Drivers.Application.Handlers;
using Drivers.Application.Ports;
using Drivers.Domain.Ports;
using Drivers.Infrastructure.Persistence;
using Drivers.Infrastructure.Services;
using EventStore.Client;
using Microsoft.EntityFrameworkCore;
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
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// SQL Server (read side)
var connectionString = builder.Configuration.GetConnectionString("ReadDb")
    ?? throw new InvalidOperationException("ReadDb connection string is missing.");

builder.Services.AddDbContext<DriversReadDbContext>(options =>
    options.UseSqlServer(connectionString));

// EventStoreDB
var eventStoreConnectionString = builder.Configuration["EventStoreDb:ConnectionString"]!;

builder.Services.AddSingleton(new EventStoreClient(
    EventStoreClientSettings.Create(eventStoreConnectionString)));

// Drivers
builder.Services.AddScoped<IDriverRepository, SqlDriverRepository>();
builder.Services.AddScoped<IDriverEventStore, EventStoreDbDriverEventStore>();
builder.Services.AddScoped<GetAvailableDriverHandler>();
builder.Services.AddScoped<OnboardDriverHandler>();
builder.Services.AddScoped<AssignDriverHandler>();
builder.Services.AddScoped<FreeDriverHandler>();

builder.Services.AddHostedService<DriverSeedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
