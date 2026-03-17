using Asp.Versioning;
using Azure.Messaging.ServiceBus;
using EventStore.Client;
using Payouts.Application.Handlers;
using Payouts.Application.Ports;
using Payouts.Infrastructure.Messaging;
using Payouts.Infrastructure.Persistence;
using Scalar.AspNetCore;

namespace Payouts.API;

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

        // Service Bus
        var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
        var payoutsTopic = builder.Configuration["ServiceBus:PayoutsTopic"]!;

        builder.Services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(serviceBusConnectionString));
        builder.Services.AddKeyedSingleton<ServiceBusSender>("payouts", (sp, _) =>
            sp.GetRequiredService<ServiceBusClient>().CreateSender(payoutsTopic));

        // Payouts
        builder.Services.AddScoped<IPayoutEventStore, EventStoreDbPayoutEventStore>();
        builder.Services.AddScoped<IPayoutEventPublisher, PayoutEventPublisher>();
        builder.Services.AddScoped<PayDriverHandler>();
        builder.Services.AddScoped<CancelPayoutHandler>();

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
