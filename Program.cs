using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess;
using TecmoTourney.Orchestration;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Bind ApplicationConfig settings
builder.Services.Configure<ApplicationConfig>(builder.Configuration.GetSection("ApplicationConfig"));
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApplicationConfig>>().Value);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


// Add the orchestrations to the dependency injection container
builder.Services.AddDataAccessServices();
builder.Services.AddOrchestrationServices();

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
