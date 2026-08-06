using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QZBarberShopBooking.API.Extensions;
using QZBarberShopBooking.API.Filters;
using QZBarberShopBooking.API.Middleware;
using QZBarberShopBooking.Infrastructure.Data;
using QZBarberShopBooking.Infrastructure.DependencyInjection;
using QZBarberShopBooking.Service.DI;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Firebase Admin — verifies the Firebase ID token the mobile app forwards after signing a user
// in with Google or Facebook. Optional: without a configured service-account key, social login
// just isn't available (SocialLoginAsync throws a clear error) instead of the API failing to
// start, so local dev without Firebase set up still works.
var firebaseKeyPath = configuration["Firebase:ServiceAccountKeyPath"];
if (!string.IsNullOrWhiteSpace(firebaseKeyPath) && FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseKeyPath)
    });
}

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

// Controllers & JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Q&Z Barber Shop API",
        Version = "v1",
        Description = "API for managing barber shop appointments and services"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Database
builder.Services.AddDbContext<BarberShopDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(BarberShopDbContext).Assembly.FullName);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Auth, infrastructure, application & service layers
builder.Services.AddJwtAuthentication(configuration, builder.Environment.IsDevelopment());
builder.Services.AddInfrastructure();
builder.Services.AddApplicationLayer();
builder.Services.AddServiceLayer();

// CORS, rate limiting & health checks
builder.Services.AddApiCors(configuration);
builder.Services.AddApiRateLimiting(configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BarberShopDbContext>(name: "database", tags: ["db", "sqlserver"]);

var app = builder.Build();

// Middleware pipeline (order matters)
app.UseExceptionHandler();

// Serves wwwroot/employee-photos (and any future wwwroot content) directly — bypasses auth
// entirely, matching the requirement that photo URLs load with no Authorization header.
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseApiCors(app.Environment);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseUserContext();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/", (IWebHostEnvironment env) =>
    env.IsDevelopment()
        ? Results.Redirect("/swagger")
        : Results.Ok(new { name = "Q&Z Barber Shop API", status = "running" }))
    .AllowAnonymous();

await app.InitializeDatabaseAsync();

app.Run();
