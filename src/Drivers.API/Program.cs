using System.Text.Json.Serialization;
using Asp.Versioning;
using Drivers.Application.Handlers;
using Drivers.Domain.Ports;
using Drivers.Infrastructure;
using Drivers.Infrastructure.Repositories;
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

var connectionString = builder.Configuration.GetConnectionString("ReadDb")
    ?? throw new InvalidOperationException("ReadDb connection string is missing.");

builder.Services.AddDbContext<ReadDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IDriverRepository, SqlDriverRepository>();
builder.Services.AddScoped<GetAvailableDriverHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
