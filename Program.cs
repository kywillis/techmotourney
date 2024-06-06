using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess;
using TecmoTourney.Orchestration;
using TecmoTourney.Orchestration.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Add the orchestrations to the dependency injection container
builder.Services.AddScoped<PlayerOrchestration, PlayerOrchestration>();
builder.Services.AddScoped<IGameResultOrchestration, GameResultOrchestration>();
builder.Services.AddScoped<ITournamentsOrchestration, TournamentsOrchestration>();
builder.Services.AddScoped<IGameResultDAO, GameResultDAO>();
builder.Services.AddScoped<ITournamentsDAO, TournamentsDAO>();

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
