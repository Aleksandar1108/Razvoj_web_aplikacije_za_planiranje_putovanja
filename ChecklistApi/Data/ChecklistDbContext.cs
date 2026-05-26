using ChecklistApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChecklistApi.Data;

public sealed class ChecklistDbContext : DbContext
{
    public ChecklistDbContext(DbContextOptions<ChecklistDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChecklistItemEntity> ChecklistItems => Set<ChecklistItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChecklistItemEntity>(e =>
        {
            e.ToTable("TravelChecklistItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.TravelPlanId, x.CreatedAtUtc }).HasDatabaseName("IX_TravelChecklistItems_TravelPlanId_CreatedAt");
        });
    }
}
