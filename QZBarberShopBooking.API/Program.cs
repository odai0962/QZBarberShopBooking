using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Infrastructure.Data;
using QZBarberShopBooking.Infrastructure.Interface;
using QZBarberShopBooking.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);

// 1. Add Services to the Container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Swagger Configuration
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "Barber Shop Booking API",
//        Version = "v1",
//        Description = "API for managing barber shop appointments and services"
//    });

//    // Add JWT Authentication to Swagger
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header using the Bearer scheme.",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            new string[] {}
//        }
//    });
//});

// 3. Database Configuration
builder.Services.AddDbContext<BarberShopDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("QZBarberShopBooking.Infrastructure");
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    });

    // Only in Development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 4. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? builder.Configuration["JWTSecretKey"] ?? "YourSuperSecretKeyHereAtLeast32CharactersLong!";

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"] ?? "BarberShopAPI",
//        ValidAudience = jwtSettings["Audience"] ?? "BarberShopClients",
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
//        ClockSkew = TimeSpan.Zero
//    };

//    options.Events = new JwtBearerEvents
//    {
//        OnAuthenticationFailed = context =>
//        {
//            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
//            {
//                context.Response.Headers.Append("Token-Expired", "true");
//            }
//            return Task.CompletedTask;
//        }
//    };
//});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("EmployeeOrAdmin", policy =>
        policy.RequireRole("Admin", "Employee"));

    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));
});

// 5. Repository Pattern & Dependency Injection
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Services (You'll create these)
//builder.Services.AddScoped<IUserService, UserService>();
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IBookingService, BookingService>();
//builder.Services.AddScoped<IServiceService, ServiceService>();
//builder.Services.AddScoped<IEmployeeService, EmployeeService>();
//builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();

//6.AutoMapper
//builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// 7. HttpContext Accessor
builder.Services.AddHttpContextAccessor();

// 8. CORS Configuration
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:4200", "https://localhost:5001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("BarberShopCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 9. Exception Handling
//builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
//builder.Services.AddProblemDetails();

// 10. Health Checks
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<BarberShopDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI(c =>
    //{
    //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Barber Shop API V1");
    //    c.RoutePrefix = "swagger";
    //});

    // Apply migrations automatically in development
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BarberShopDbContext>();
        dbContext.Database.Migrate();
    }
}

app.UseHttpsRedirection();
app.UseCors("BarberShopCors");
app.UseAuthentication();
app.UseAuthorization();

// Exception handler must be after CORS but before endpoints
app.UseExceptionHandler();

// Health Check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();