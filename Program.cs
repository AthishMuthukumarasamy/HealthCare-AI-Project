
using Healthcare.API.Services;
using Healthcare.API.Data;
using Healthcare.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration
builder.Services.AddDbContext<HealthcareDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HealthcareConnection")));

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<HealthcareDbContext>()
    .AddDefaultTokenProviders();

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});

// HTTP Logging Configuration
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestProperties |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
});

// Controllers and Validation Configuration
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                        .ToArray()
                );

            var response = new
            {
                status = StatusCodes.Status400BadRequest,
                message = "One or more validation errors occurred.",
                errors,
                traceId = context.HttpContext.TraceIdentifier
            };

            return new BadRequestObjectResult(response);
        };
    });

// Swagger Configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HealthConnect AI API",
        Version = "v1",
        Description = "API documentation for the HealthConnect AI healthcare management system."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Swagger will add the Bearer prefix automatically."
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

// Dependency Injection
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();

var app = builder.Build();

// Create default Identity roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Doctor", "Patient" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Failed to create role {role}: " +
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }

            Console.WriteLine($"Role created: {role}");
        }
    }
}

// Bootstrap the first Admin account (Development only)
Console.WriteLine(
    $"Current environment: {app.Environment.EnvironmentName}");

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("Development Admin bootstrap started.");

    using (var scope = app.Services.CreateScope())
    {
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmail = builder.Configuration[
            "BootstrapAdmin:Email"];

        var adminPassword = builder.Configuration[
            "BootstrapAdmin:Password"];

        Console.WriteLine(
            $"Bootstrap Admin email: {adminEmail ?? "NULL"}");

        Console.WriteLine(
            $"Bootstrap Admin password configured: " +
            $"{!string.IsNullOrWhiteSpace(adminPassword)}");

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            Console.WriteLine(
                "Admin bootstrap skipped: configuration is missing.");
        }
        else
        {
            var adminUser = await userManager
                .FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    FullName = "System Administrator",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    adminUser, adminPassword);

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Failed to create bootstrap Admin account: " +
                        string.Join(", ",
                            result.Errors.Select(e => e.Description)));
                }

                Console.WriteLine("Admin account created successfully.");
            }
            else
            {
                Console.WriteLine("Admin account already exists.");
            }

            if (!await userManager.IsInRoleAsync(
                adminUser, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    adminUser, "Admin");

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        "Failed to assign Admin role: " +
                        string.Join(", ",
                            roleResult.Errors.Select(e => e.Description)));
                }

                Console.WriteLine("Admin role assigned successfully.");
            }
            else
            {
                Console.WriteLine("Admin already has the Admin role.");
            }
        }
    }
}

// Centralised Exception Handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        var exception = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()
            ?.Error;

        logger.LogError(
            exception,
            "An unexpected error occurred while processing the request.");

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = StatusCodes.Status500InternalServerError,
            message = "An unexpected error occurred. Please try again later.",
            traceId = context.TraceIdentifier
        });
    });
});

// HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "HealthConnect AI API v1");

        options.RoutePrefix = "swagger";
    });
}

// HTTP Request Logging
app.UseHttpLogging();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", async (
    IServiceScopeFactory scopeFactory,
    ILogger<Program> logger) =>
{
    var databaseStatus = "Unhealthy";
    var databaseHealthy = false;

    try
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<HealthcareDbContext>();

        databaseHealthy = await dbContext.Database.CanConnectAsync();

        databaseStatus = databaseHealthy ? "Healthy" : "Unhealthy";
    }
    catch (Exception exception)
    {
        logger.LogWarning(
            exception,
            "Database health check failed.");
    }

    var overallHealthy = databaseHealthy;

    var response = new
    {
        status = overallHealthy ? "Healthy" : "Unhealthy",
        checks = new
        {
            api = "Healthy",
            database = databaseStatus
        },
        timestamp = DateTime.UtcNow
    };

    return Results.Json(
        response,
        statusCode: overallHealthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable);
});

app.Run();
