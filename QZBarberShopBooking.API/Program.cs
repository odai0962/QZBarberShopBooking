using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QZBarberShopBooking.Infrastructure.Data;
using QZBarberShopBooking.Infrastructure.Interface;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Swagger Configuration
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
        Description = "Enter 'Bearer' followed by your JWT token"
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
});

// 3. Database
builder.Services.AddDbContext<BarberShopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. JWT Authentication 
builder.Services.AddJwtAuthentication(builder.Configuration);

// 5. Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 6. CORS
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// 7. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BarberShopDbContext>(
        name: "database",
        tags: new[] { "db", "sqlserver" })
    .AddUrlGroup(new Uri("https://google.com"), name: "External API");

var app = builder.Build();

// 8. Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Q&Z Barber Shop API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 9. Health endpoint 
app.MapGet("/health", () => Results.Json(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Database = "Connected"
}));

app.MapHealthChecks("/healthz");

app.Run();