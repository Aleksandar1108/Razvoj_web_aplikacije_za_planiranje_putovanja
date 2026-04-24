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
    public DbSet<TravelPlanRowEntity> TravelPlans => Set<TravelPlanRowEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TravelPlanRowEntity>(e =>
        {
            e.ToTable("TravelPlans");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ChecklistItemEntity>(e =>
        {
            e.ToTable("TravelChecklistItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.TravelPlanId, x.CreatedAtUtc }).HasDatabaseName("IX_TravelChecklistItems_TravelPlanId_CreatedAt");

            e.HasOne<TravelPlanRowEntity>()
                .WithMany()
                .HasForeignKey(x => x.TravelPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
