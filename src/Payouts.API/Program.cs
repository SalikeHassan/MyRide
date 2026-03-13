using Asp.Versioning;
using Azure.Messaging.ServiceBus;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
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
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddMvc();

        // MongoDB
        var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]!;
        var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]!;

        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        builder.Services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

        // Service Bus
        var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
        var payoutsTopic = builder.Configuration["ServiceBus:PayoutsTopic"]!;

        builder.Services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(serviceBusConnectionString));
        builder.Services.AddKeyedSingleton<ServiceBusSender>("payouts", (sp, _) =>
            sp.GetRequiredService<ServiceBusClient>().CreateSender(payoutsTopic));

        // Payouts
        builder.Services.AddScoped<IPayoutEventStore, MongoPayoutEventStore>();
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