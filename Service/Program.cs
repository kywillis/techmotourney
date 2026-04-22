using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TecmoTourney.DataAccess;
using TecmoTourney.Orchestration;
using TecmoTourney;
using TecmoTourney.Middleware;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

// Register Dapper type handlers for wager enums (stored as string in DB)
WagerDapperRegistration.RegisterWagerEnumHandlers();

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
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Bind ApplicationConfig settings
builder.Services.Configure<ApplicationConfig>(builder.Configuration.GetSection("ApplicationConfig"));
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApplicationConfig>>().Value);

// Bind Azure AI settings (for odds generation)
builder.Services.Configure<AzureAIOptions>(builder.Configuration.GetSection(AzureAIOptions.SectionName));
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AzureAIOptions>>().Value);

// Bind Google auth (for wager app)
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection(GoogleAuthOptions.SectionName));

// Add Authentication: Google ID tokens (JWT) for wager endpoints
var googleAuth = builder.Configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>();
if (!string.IsNullOrEmpty(googleAuth?.ClientId))
{
    builder.Services.AddAuthentication()
    .AddJwtBearer("Google", options =>
    {
        options.Authority = "https://accounts.google.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://accounts.google.com",
            ValidAudiences = new[] { googleAuth.ClientId },
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
}

// Add CORS policy (existing tecmo-tourney + wager app origins)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder => policyBuilder
            .WithOrigins("http://localhost:4200", "http://localhost:4201")
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.UseTournamentsWriteAdmin();
app.UseWagerPlayerResolution();

// API routes first; SPA fallback last so /api/* is never served index.html
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
