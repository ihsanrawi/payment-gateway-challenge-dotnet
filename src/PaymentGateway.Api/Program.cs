using FluentValidation;
using FluentValidation.AspNetCore;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Repository;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders()
    .AddConsole();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PaymentsRepository>();
// FluentValidation automatic integration
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<PostPaymentRequestValidator>();

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
