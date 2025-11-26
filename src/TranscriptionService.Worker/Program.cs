using Microsoft.EntityFrameworkCore;
using TranscriptionService.Application;
using TranscriptionService.Infrastructure;
using TranscriptionService.Infrastructure.Persistence;
using TranscriptionService.Worker;
using TranscriptionService.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("TranscriptionService", LogLevel.Information);

// Add Application layer (MediatR, validators, etc.)
builder.Services.AddApplication();

// Add Infrastructure layer (database, services, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add the Worker service
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<WorkerMetricsService>();

var host = builder.Build();

// Run database migrations
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("✓ Database migration completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗ An error occurred while migrating the database");
        throw;
    }
}

await host.RunAsync();
