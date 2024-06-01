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
