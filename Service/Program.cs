using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TecmoTourney.DataAccess;
using TecmoTourney.Orchestration;
using TecmoTourney;
using TecmoTourney.Middleware;
using TecmoTourney.Notifications;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
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

builder.Services.Configure<NtfyOptions>(builder.Configuration.GetSection(NtfyOptions.SectionName));
builder.Services.AddHttpClient<INtfyClient, NtfyClient>();

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

// Add CORS policy (local dev + optional Static Web Apps + same-site host)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder => policyBuilder
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201",
                "https://tecmo.azurewebsites.net",
                "https://happy-bush-03052ce1e.7.azurestaticapps.net",
                "https://purple-cliff-0df052a1e.7.azurestaticapps.net")
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

// One-time: verify Ntfy resolved from configuration (overrides: env Ntfy__Topic, user secrets, etc.)
{
    var raw = app.Configuration["Ntfy:Topic"];
    var o = app.Services.GetRequiredService<IOptions<NtfyOptions>>().Value;
    app.Logger.LogInformation(
        "Ntfy: Configuration[Ntfy:Topic] = {Raw} | IOptions.Topic = {BoundTopic} | BaseUrl = {BaseUrl}",
        raw == null ? "<null key>" : raw == "" ? "<empty string>" : raw,
        string.IsNullOrEmpty(o.Topic) ? "<empty>" : o.Topic!,
        o.BaseUrl);
}

var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

// Shared wwwroot (legacy assets: jquery, bracket, etc.)
app.UseStaticFiles();

// SPAs built to wwwroot/wager and wwwroot/tourney (base-href /wager/ and /tourney/)
var wagerPath = Path.Combine(webRoot, "wager");
if (Directory.Exists(wagerPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(wagerPath),
        RequestPath = "/wager"
    });
}

var tourneyPath = Path.Combine(webRoot, "tourney");
if (Directory.Exists(tourneyPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(tourneyPath),
        RequestPath = "/tourney"
    });
}

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

app.MapControllers();

// Default site root: send users to tournament SPA (change if you prefer a landing page)
app.MapGet("/", () => Results.Redirect("/tourney/"));

// Deep links and refresh on client routes
if (Directory.Exists(wagerPath))
{
    app.MapFallbackToFile("/wager/{*path:nonfile}", "wager/index.html");
}

if (Directory.Exists(tourneyPath))
{
    app.MapFallbackToFile("/tourney/{*path:nonfile}", "tourney/index.html");
}

app.Run();
