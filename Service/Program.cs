using TecmoTourney.DataAccess;
using TecmoTourney.Orchestration;
using TecmoTourney;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Add appsettings.local.json
builder.Configuration
    .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Bind ApplicationConfig settings
builder.Services.Configure<ApplicationConfig>(builder.Configuration.GetSection("ApplicationConfig"));
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApplicationConfig>>().Value);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("http://localhost:4200") // Angular app URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Add the orchestrations to the dependency injection container
builder.Services.AddDataAccessServices();
builder.Services.AddOrchestrationServices();

var app = builder.Build();

// Serve Angular files from wwwroot (or another folder if needed)
app.UseDefaultFiles(); // Looks for index.html
app.UseStaticFiles();  // Serves static assets like JS, CSS
app.MapFallbackToFile("index.html");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
app.UseAuthorization();

// API routes
app.MapControllers();
app.Run();
