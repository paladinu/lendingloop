using MongoDB.Driver;
using Api.Services;

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
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(fullUploadPath),
    RequestPath = $"/{uploadPath}"
});

// Use CORS
app.UseCors("AllowAngularDev");

// Map controllers
app.MapControllers();

app.Run();
