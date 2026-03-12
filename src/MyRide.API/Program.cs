using Azure.Messaging.ServiceBus;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Payments.Application.Handlers;
using Payments.Application.Ports;
using Payments.Infrastructure.Messaging;
using Payments.Infrastructure.Persistence;
using Payouts.Application.Handlers;
using Payouts.Application.Ports;
using Payouts.Infrastructure.Messaging;
using Payouts.Infrastructure.Persistence;
using Rides.Application.Handlers;
using Rides.Application.Ports;
using Rides.Infrastructure.Messaging;
using Rides.Infrastructure.Persistence;
using Scalar.AspNetCore;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// MongoDB
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]!;
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]!;

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

// Service Bus
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
var ridesTopic = builder.Configuration["ServiceBus:RidesTopic"]!;
var paymentsTopic = builder.Configuration["ServiceBus:PaymentsTopic"]!;
var payoutsTopic = builder.Configuration["ServiceBus:PayoutsTopic"]!;

builder.Services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddKeyedSingleton<ServiceBusSender>("rides", (sp, _) =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(ridesTopic));
builder.Services.AddKeyedSingleton<ServiceBusSender>("payments", (sp, _) =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(paymentsTopic));
builder.Services.AddKeyedSingleton<ServiceBusSender>("payouts", (sp, _) =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(payoutsTopic));

// Rides
builder.Services.AddScoped<IRideEventStore, MongoRideEventStore>();
builder.Services.AddScoped<IRideEventPublisher, RideEventPublisher>();
builder.Services.AddScoped<IRideReadStore, MongoRideReadStore>();
builder.Services.AddScoped<StartRideHandler>();
builder.Services.AddScoped<AcceptRideHandler>();
builder.Services.AddScoped<CompleteRideHandler>();
builder.Services.AddScoped<CancelRideHandler>();

// Payments
builder.Services.AddScoped<IPaymentEventStore, MongoPaymentEventStore>();
builder.Services.AddScoped<IPaymentEventPublisher, PaymentEventPublisher>();
builder.Services.AddScoped<ChargeRiderHandler>();
builder.Services.AddScoped<RefundRiderHandler>();

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

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
