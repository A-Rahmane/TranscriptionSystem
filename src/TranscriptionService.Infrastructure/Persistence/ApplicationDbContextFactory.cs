using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TranscriptionService.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden at runtime by appsettings.json
        optionsBuilder.UseMySql(
            "Server=localhost;Database=transcription_db;User=root;Password=password;",
            ServerVersion.AutoDetect("Server=localhost;Database=transcription_db;User=root;Password=password;"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
