using MongoDB.Driver;
using Api.Services;
using Api.Scripts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Controllers with JSON options to serialize enums as strings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("https://local-www.lendingloop.com:4200", "http://localhost:4200", "http://localhost:4201")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Allow credentials for authentication
    });
});

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var requireHttpsMetadata = builder.Configuration.GetValue<bool>("Jwt:RequireHttpsMetadata");
var saveToken = builder.Configuration.GetValue<bool>("Jwt:SaveToken");
var requireExpirationTime = builder.Configuration.GetValue<bool>("Jwt:RequireExpirationTime");
var validateLifetime = builder.Configuration.GetValue<bool>("Jwt:ValidateLifetime");

if (!string.IsNullOrEmpty(jwtSecretKey) && !string.IsNullOrEmpty(jwtIssuer) && !string.IsNullOrEmpty(jwtAudience))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.SaveToken = saveToken;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = validateLifetime,
            RequireExpirationTime = requireExpirationTime,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        
        // Configure events for better error handling
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticatedUser", policy =>
        {
            policy.RequireAuthenticatedUser();
        });
    });
}

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? builder.Configuration["MongoDB:ConnectionString"];
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"];

if (!string.IsNullOrEmpty(mongoConnectionString) && !string.IsNullOrEmpty(mongoDatabaseName))
{
    builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
    {
        return new MongoClient(mongoConnectionString);
    });

    builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
    {
        var client = serviceProvider.GetRequiredService<IMongoClient>();
        return client.GetDatabase(mongoDatabaseName);
    });

    // Register TagsService
    builder.Services.AddSingleton<ITagsService, TagsService>();
    
    // Register ItemsService
    builder.Services.AddScoped<IItemsService, ItemsService>();
    
    // Register ItemRequestService
    builder.Services.AddScoped<IItemRequestService, ItemRequestService>();
    
    // Register UserService
    builder.Services.AddScoped<IUserService, UserService>();
    
    // Register Password Service
    builder.Services.AddScoped<IPasswordService, PasswordService>();
    
    // Register JWT Token Service
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    
    // Register Email Service
    builder.Services.AddScoped<IEmailService, EmailService>();
    
    // Register Loop Service
    builder.Services.AddScoped<ILoopService, LoopService>();
    
    // Register Loop Invitation Service
    builder.Services.AddScoped<ILoopInvitationService, LoopInvitationService>();
    
    // Register Loop Join Request Service
    builder.Services.AddScoped<ILoopJoinRequestService, LoopJoinRequestService>();
    
    // Register Notification Service
    builder.Services.AddScoped<INotificationService, NotificationService>();
    
    // Register LoopScore Service
    builder.Services.AddScoped<ILoopScoreService, LoopScoreService>();
    
    // Register Database Migration Service
    builder.Services.AddScoped<DatabaseMigration>();
    
    // Set up dependencies for LoopService to avoid circular dependency
    builder.Services.AddScoped(serviceProvider =>
    {
        var loopService = serviceProvider.GetRequiredService<ILoopService>() as LoopService;
        var itemsService = serviceProvider.GetRequiredService<IItemsService>();
        var loopInvitationService = serviceProvider.GetRequiredService<ILoopInvitationService>();
        var loopJoinRequestService = serviceProvider.GetRequiredService<ILoopJoinRequestService>();
        
        loopService?.SetDependencies(itemsService, loopInvitationService, loopJoinRequestService);
        return loopService!;
    });
    
    // Migration endpoints are automatically available through controller registration
}

var app = builder.Build();

// Configure Kestrel for HTTPS with custom domain
if (app.Environment.IsDevelopment())
{
    var certPath = builder.Configuration["Kestrel:Endpoints:Https:Certificate:Path"];
    var certPassword = builder.Configuration["Kestrel:Endpoints:Https:Certificate:Password"];
    
    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        app.Logger.LogInformation($"Using SSL certificate from: {certPath}");
    }
    else if (!string.IsNullOrEmpty(certPath))
    {
        app.Logger.LogWarning($"SSL certificate not found at: {certPath}. HTTPS may not work correctly.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure static file serving for uploaded images
var uploadPath = builder.Configuration["FileStorage:UploadPath"] ?? "uploads/images";
var fullUploadPath = Path.Combine(app.Environment.ContentRootPath, uploadPath);
Directory.CreateDirectory(fullUploadPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fullUploadPath),
    RequestPath = $"/{uploadPath}"
});

// Explicitly handle OPTIONS requests for CORS preflight - BEFORE UseCors
app.Use(async (context, next) =>
{
    app.Logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
    
    if (context.Request.Method == "OPTIONS")
    {
        app.Logger.LogInformation("OPTIONS request detected, processing...");
        // Let CORS middleware handle it, but ensure we return 200
        await next();
        app.Logger.LogInformation($"After CORS, status code: {context.Response.StatusCode}");
        if (context.Response.StatusCode == 404)
        {
            app.Logger.LogInformation("Changing 404 to 200 for OPTIONS");
            context.Response.StatusCode = 200;
        }
        return;
    }
    await next();
});

// Use CORS - MUST be early in pipeline
app.UseCors("AllowAngularDev");

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers - this implicitly adds routing
app.MapControllers();

// Initialize default tags on startup
using (var scope = app.Services.CreateScope())
{
    var tagsService = scope.ServiceProvider.GetRequiredService<ITagsService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Initializing default system tags");
        await tagsService.InitializeDefaultTagsAsync();
        logger.LogInformation("Default system tags initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize default system tags");
        // Don't fail the application startup, just log the error
    }
}

// Auto-run migration if configured
var autoRunMigration = builder.Configuration.GetValue<bool>("Migration:AutoRunOnStartup");
if (autoRunMigration)
{
    using (var scope = app.Services.CreateScope())
    {
        var migration = scope.ServiceProvider.GetRequiredService<DatabaseMigration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Auto-running database migration on startup");
            await migration.RunCompleteMigration();
            logger.LogInformation("Database migration completed successfully on startup");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run database migration on startup");
            // Don't fail the application startup, just log the error
        }
    }
}

app.Run();
