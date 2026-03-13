using Asp.Versioning;
using Azure.Messaging.ServiceBus;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Rides.Application.Handlers;
using Rides.Application.Ports;
using Rides.Infrastructure.Messaging;
using Rides.Infrastructure.Persistence;
using Scalar.AspNetCore;

namespace Rides.API;

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
        var ridesTopic = builder.Configuration["ServiceBus:RidesTopic"]!;

        builder.Services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(serviceBusConnectionString));
        builder.Services.AddKeyedSingleton<ServiceBusSender>("rides", (sp, _) =>
            sp.GetRequiredService<ServiceBusClient>().CreateSender(ridesTopic));

        // Rides
        builder.Services.AddScoped<IRideEventStore, MongoRideEventStore>();
        builder.Services.AddScoped<IRideEventPublisher, RideEventPublisher>();
        builder.Services.AddScoped<IRideReadStore, MongoRideReadStore>();
        builder.Services.AddScoped<StartRideHandler>();
        builder.Services.AddScoped<AcceptRideHandler>();
        builder.Services.AddScoped<CompleteRideHandler>();
        builder.Services.AddScoped<CancelRideHandler>();

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