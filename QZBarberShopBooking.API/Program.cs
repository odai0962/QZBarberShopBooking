using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using QZBarberShopBooking.API.Middleware;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Extensions;
using QZBarberShopBooking.Application.Helpers;  // ✅ إضافة هذا الـ using
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Infrastructure.Authentication;
using QZBarberShopBooking.Infrastructure.Data;
using QZBarberShopBooking.Infrastructure.Interface;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Service.DI;
using QZBarberShopBooking.Service.Service.Auth;
using QZBarberShopBooking.Service.Service.User;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ========== 1. CONFIGURATION SETUP ==========
var configuration = builder.Configuration;

// ========== 2. LOGGING CONFIGURATION ==========
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

// ========== 3. SERVICES REGISTRATION ==========

// 3.1 Controllers with JSON options
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

// 3.2 Swagger Configuration (مبسطة)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Q&Z Barber Shop API",
        Version = "v1",
        Description = "API for managing barber shop appointments and services"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: 'Bearer {your_token}'"
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

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 3.3 Database Configuration
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

// 3.4 JWT Authentication
builder.Services.AddJwtAuthentication(configuration);

// 3.5 Application Layer
builder.Services.AddApplicationLayer();

// 3.6 Service Layer Registration
var serviceAssembly = Assembly.GetAssembly(typeof(AuthService));
if (serviceAssembly != null)
{
    builder.Services.AddScopedServicesFromAssembly(serviceAssembly);
}

// Manual Service Registration for Interfaces
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// 3.7 Repositories & Unit of Work
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 3.8 HttpContext Accessor (مهم لـ UserContext)
builder.Services.AddHttpContextAccessor();

// 3.9 CORS Configuration (مبسطة)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3.10 Health Checks (مبسطة)
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<BarberShopDbContext>(
//        name: "database",
//        tags: new[] { "db", "sqlserver" })
//    .AddSqlServer(
//        configuration.GetConnectionString("DefaultConnection")!,
//        name: "sqlserver",
//        tags: new[] { "db", "sqlserver" });

var app = builder.Build();

// ========== 4. تهيئة UserContext ==========
app.UseMiddleware<UserContextMiddleware>();

// ========== 5. MIDDLEWARE PIPELINE ==========

// 5.1 Development Environment Setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
    app.UseHttpsRedirection();
}

// 5.2 Global Exception Handling Middleware
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exception, "Unhandled exception occurred");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(
            exception?.Message ?? "An unexpected error occurred",
            "An unexpected error occurred"
        );

        if (exception != null && app.Environment.IsDevelopment())
        {
            response.Errors = new List<string> { exception.Message, exception.StackTrace ?? "" };
        }

        await context.Response.WriteAsJsonAsync(response);
    });
});

// 5.3 Custom Middleware
app.UseMiddleware<AuthorizeMiddleware>();

// 5.4 Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// ========== 6. ENDPOINT MAPPING ==========

// 6.1 Map Controllers
app.MapControllers();

// 6.2 Health Check Endpoints
app.MapHealthChecks("/health").AllowAnonymous();

// 6.3 Minimal API Endpoints
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

// ========== 7. DATABASE SEEDING ==========
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BarberShopDbContext>();

    try
    {
        await dbContext.Database.EnsureCreatedAsync();

        // Seed initial data إذا كان هناك SeedData
        // await SeedData.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}

app.Run();