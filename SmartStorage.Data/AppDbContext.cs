using Microsoft.EntityFrameworkCore;
using SmartStorage.Data.Entities;

namespace SmartStorage.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();
    public DbSet<ActionLog> ActionLogs => Set<ActionLog>();
    public DbSet<RecommendationLog> RecommendationLogs => Set<RecommendationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileEntity>().HasIndex(x => x.Path).IsUnique();
        modelBuilder.Entity<UsageLog>().HasIndex(x => new { x.Path, x.AccessedUtc });
        modelBuilder.Entity<ActionLog>().HasIndex(x => x.ExecutedUtc);
        modelBuilder.Entity<RecommendationLog>().HasIndex(x => x.LoggedUtc);
    }
}
