using ActivitiesApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActivitiesApi.Data;

public sealed class ActivitiesDbContext : DbContext
{
    public ActivitiesDbContext(DbContextOptions<ActivitiesDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelActivityEntity> TravelActivities => Set<TravelActivityEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TravelActivityEntity>(e =>
        {
            e.ToTable("TravelActivities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ActivityDate).HasColumnType("date");
            e.Property(x => x.ActivityTime).HasMaxLength(5).IsRequired();
            e.Property(x => x.Location).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.TravelPlanId, x.ActivityDate, x.ActivityTime })
                .HasDatabaseName("IX_TravelActivities_TravelPlanId_Date_Time");
        });
    }
}
