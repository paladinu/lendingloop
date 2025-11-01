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

// Add Controllers
builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
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
    
    // Register Database Migration Service
    builder.Services.AddScoped<DatabaseMigration>();
    
    // Migration endpoints are automatically available through controller registration
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure static file serving for uploaded images
var uploadPath = builder.Configuration["FileStorage:UploadPath"] ?? "uploads/images";
var fullUploadPath = Path.Combine(app.Environment.ContentRootPath, uploadPath);
Directory.CreateDirectory(fullUploadPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fullUploadPath),
    RequestPath = $"/{uploadPath}"
});

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Use CORS
app.UseCors("AllowAngularDev");

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

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
