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
    public DbSet<TravelPlanRowEntity> TravelPlans => Set<TravelPlanRowEntity>();
    public DbSet<TravelPlanShareLinkEntity> TravelPlanShareLinks => Set<TravelPlanShareLinkEntity>();
    public DbSet<TravelPlanShareRecipientEntity> TravelPlanShareRecipients => Set<TravelPlanShareRecipientEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TravelPlanRowEntity>(e =>
        {
            e.ToTable("TravelPlans");
            e.HasKey(x => x.Id);
            e.Property(x => x.StartDate).HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnType("date");
        });

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

            e.HasOne<TravelPlanRowEntity>()
                .WithMany()
                .HasForeignKey(x => x.TravelPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TravelPlanShareLinkEntity>(e =>
        {
            e.ToTable("TravelPlanShareLinks");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasColumnType("varbinary(32)").IsRequired();
            e.Property(x => x.Permission).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();

            e.HasOne<TravelPlanRowEntity>()
                .WithMany()
                .HasForeignKey(x => x.TravelPlanId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TravelPlanShareRecipientEntity>(e =>
        {
            e.ToTable("TravelPlanShareRecipients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Permission).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.TravelPlanId, x.RecipientUserId }).IsUnique();

            e.HasOne<TravelPlanRowEntity>()
                .WithMany()
                .HasForeignKey(x => x.TravelPlanId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
