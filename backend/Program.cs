using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Data;
using AuthenticationApi.Common.Services;
using AuthenticationApi.Common.Middleware;
using AuthenticationApi.Common.HealthChecks;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Security.Cryptography;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Data Protection API for secure storage of sensitive data
var keyStoragePath = builder.Configuration.GetValue<string>("DataProtection:KeyStoragePath") 
    ?? Path.Combine(builder.Environment.ContentRootPath, "Keys");
Directory.CreateDirectory(keyStoragePath);

builder.Services.AddDataProtection()
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // 90-day key rotation

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<AuthenticationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Add JWT configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Add authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings != null)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                // RSA key validation will be handled by the JWT token service
                ClockSkew = TimeSpan.Zero
            };
        });
}

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Add ASP.NET Core Rate Limiting
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Login endpoint: 5 attempts per 15 minutes (fixed window, per username)
    rateLimiterOptions.AddFixedWindowLimiter("LoginPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(15);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0; // No queuing for security endpoints
    });

    // Refresh endpoint: 10 attempts per minute (sliding window, per IP)
    rateLimiterOptions.AddSlidingWindowLimiter("RefreshPolicy", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.SegmentsPerWindow = 6; // 10-second segments
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // MFA Verify: 5 attempts per 5 minutes (fixed window, per user)
    rateLimiterOptions.AddFixedWindowLimiter("MfaPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(5);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // General API: 100 attempts per minute (sliding window, per IP)
    rateLimiterOptions.AddSlidingWindowLimiter("GeneralPolicy", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.SegmentsPerWindow = 6; // 10-second segments
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter("GlobalLimiter", _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            }));

    rateLimiterOptions.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.Add("Retry-After", "60");
        return new ValueTask();
    };
});

// Add services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IAuditService>(provider =>
{
    var baseAuditService = provider.GetRequiredService<AuditService>();
    var metricsService = provider.GetRequiredService<MetricsService>();
    var logger = provider.GetRequiredService<ILogger<EnhancedAuditService>>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    
    return new EnhancedAuditService(baseAuditService, metricsService, logger, httpContextAccessor);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<BusinessMetricsService>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add background services
builder.Services.AddHostedService<SessionCleanupService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuthenticationDbContext>();

// Add background service health checks
builder.Services.AddBackgroundServiceHealthCheck();

// Add CORS with production-ready configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Configure allowed origins based on environment
        var allowedOrigins = new List<string>();
        
        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins.AddRange(new[]
            {
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3001"
            });
        }
        
        // Add production origins from configuration
        var productionOrigins = builder.Configuration.GetSection("Security:Cors:AllowedOrigins").Get<string[]>();
        if (productionOrigins != null)
        {
            allowedOrigins.AddRange(productionOrigins);
        }
        
        policy.WithOrigins(allowedOrigins.ToArray())
              .WithHeaders(
                  "Content-Type",
                  "Authorization",
                  "X-Requested-With",
                  "Accept",
                  "Origin",
                  "X-Correlation-Id"
              )
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)) // Cache preflight for 24 hours
              .WithExposedHeaders(
                  "X-RateLimit-Policy",
                  "X-RateLimit-Limit",
                  "X-RateLimit-Remaining",
                  "X-RateLimit-Reset",
                  "X-RateLimit-Window",
                  "X-Correlation-Id",
                  "Retry-After"
              );
    });
    
    // Add a stricter policy for production API endpoints
    options.AddPolicy("ProductionApi", policy =>
    {
        var productionOrigins = builder.Configuration.GetSection("Security:Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://app.translution.com" };
            
        policy.WithOrigins(productionOrigins)
              .WithHeaders(
                  "Content-Type",
                  "Authorization",
                  "X-Requested-With",
                  "Accept",
                  "Origin"
              )
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(86400))
              .WithExposedHeaders(
                  "X-RateLimit-Policy",
                  "X-RateLimit-Limit",
                  "X-RateLimit-Remaining",
                  "X-RateLimit-Reset",
                  "X-RateLimit-Window",
                  "X-Correlation-Id"
              );
    });
    
    // Add a policy for health checks and monitoring
    options.AddPolicy("Monitoring", policy =>
    {
        policy.WithOrigins("*")
              .WithHeaders("Content-Type", "Accept")
              .WithMethods("GET")
              .SetPreflightMaxAge(TimeSpan.FromSeconds(86400));
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Authentication API",
        Version = "v1",
        Description = "JWT-based authentication API for TransLution system. Provides secure user authentication, session management, and token operations with ISO 27001 compliance.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TransLution Engineering Team"
        }
    });

    // Include XML comments for documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add JWT Bearer authentication scheme
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add security middleware (order is important)
app.UseCorrelationId();
app.UseRequestLogging();
app.UseSecurityHeaders();

app.UseCors("AllowFrontend");

// Add rate limiting middleware
app.UseRateLimiter();
app.UseRateLimitHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map monitoring endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration.TotalMilliseconds,
                description = x.Value.Description,
                exception = x.Value.Exception?.Message
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Map Prometheus metrics endpoint
if (builder.Configuration.GetValue<bool>("Monitoring:EnableMetrics", true))
{
    app.MapMetrics("/metrics");
}

// Apply database migrations on startup (configurable for production)
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
    context.Database.Migrate();

    // Seed development data
    if (app.Environment.IsDevelopment())
    {
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        await AuthenticationApi.Common.Data.SeedData.SeedDevelopmentDataAsync(context, userService, app.Environment);
    }
}

app.Run();
