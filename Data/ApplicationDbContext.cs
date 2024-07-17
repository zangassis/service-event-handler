using Microsoft.EntityFrameworkCore;
using ServiceEventHandler.Models;

namespace ServiceEventHandler.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Service> Services { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<ServiceIntegrationError> ServiceIntegrationErrors { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Log>()
            .Property(l => l.LogLevel)
            .HasConversion<string>();
    }
}
