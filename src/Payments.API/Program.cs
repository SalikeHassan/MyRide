using Asp.Versioning;
using EventStore.Client;
using Payments.Application.Handlers;
using Payments.Application.Ports;
using Payments.Infrastructure.Persistence;
using Scalar.AspNetCore;

namespace Payments.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc();

        // EventStoreDB
        var eventStoreConnectionString = builder.Configuration["EventStoreDb:ConnectionString"]!;

        builder.Services.AddSingleton(new EventStoreClient(
            EventStoreClientSettings.Create(eventStoreConnectionString)));

        // Payments
        builder.Services.AddScoped<IPaymentEventStore, EventStoreDbPaymentEventStore>();
        builder.Services.AddScoped<ChargeRiderHandler>();
        builder.Services.AddScoped<RefundRiderHandler>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
