using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Domain.Interfaces;
using TranscriptionService.Infrastructure.Configuration;
using TranscriptionService.Infrastructure.Persistence;
using TranscriptionService.Infrastructure.Persistence.Repositories;
using TranscriptionService.Infrastructure.Services;

namespace TranscriptionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration Options
        services.Configure<WhisperOptions>(
            configuration.GetSection(WhisperOptions.SectionName));
        
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));
        
        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

        // Repositories
        services.AddScoped<ITranscriptionJobRepository, TranscriptionJobRepository>();

        // Services
        services.AddSingleton<IWhisperService, WhisperCppService>();
        services.AddSingleton<IMessageQueue, RabbitMqMessageQueue>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // Background Services *removed*
        // services.AddHostedService<BackgroundJobService>();

        return services;
    }
}
