using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.Extensions.Options;

using PaymentGateway.Api.Validators;
using PaymentGateway.Application.Services;
using PaymentGateway.Domain.Configs;
using PaymentGateway.Domain.Mappers;
using PaymentGateway.Infrastructure.External;
using PaymentGateway.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders()
    .AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BankSimulatorConfigs>(builder.Configuration.GetSection("BankSimulator"));
builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddSingleton<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<IPaymentProcessorService, PaymentProcessorService>();
builder.Services.AddHttpClient<IBankClient, BankClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BankSimulatorConfigs>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

// Register validators (manual validation - no auto-validation)
builder.Services.AddValidatorsFromAssemblyContaining<PostPaymentRequestValidator>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(PaymentMappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
