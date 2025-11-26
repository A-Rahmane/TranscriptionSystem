using Microsoft.EntityFrameworkCore;
using TranscriptionService.API.GrpcServices;
using TranscriptionService.API.Middleware;
using TranscriptionService.Application;
using TranscriptionService.Infrastructure;
using TranscriptionService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger/OpenAPI
// builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Transcription Service API",
        Version = "v1",
        Description = "ASP.NET API for audio transcription using Whisper.cpp",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Transcription Service",
            Email = "support@transcriptionservice.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// AutoMapper for API
builder.Services.AddAutoMapper(typeof(Program));

// gRPC
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();
    
// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Transcription Service API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}


// Custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map gRPC services
app.MapGrpcService<TranscriptionGrpcService>();

// gRPC reflection (for tools like grpcurl)
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Health check endpoint
app.MapHealthChecks("/health");

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
    }
}

app.Logger.LogInformation("Transcription Service API starting...");
app.Logger.LogInformation("REST API available at: {Url}", app.Urls.FirstOrDefault() ?? "http://localhost:5000");
app.Logger.LogInformation("gRPC service available at: {Url}", app.Urls.FirstOrDefault() ?? "http://localhost:5000");

app.Run();
