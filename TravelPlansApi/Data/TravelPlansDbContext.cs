using Microsoft.EntityFrameworkCore;
using TravelPlansApi.Data.Entities;

namespace TravelPlansApi.Data;

public sealed class TravelPlansDbContext : DbContext
{
    public TravelPlansDbContext(DbContextOptions<TravelPlansDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelPlanEntity> TravelPlans => Set<TravelPlanEntity>();
    public DbSet<TravelPlanShareLinkEntity> TravelPlanShareLinks => Set<TravelPlanShareLinkEntity>();
    public DbSet<TravelPlanShareRecipientEntity> TravelPlanShareRecipients => Set<TravelPlanShareRecipientEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TravelPlanEntity>(e =>
        {
            e.ToTable("TravelPlans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(500).IsRequired();
            e.Property(x => x.StartDate).HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnType("date");
            e.Property(x => x.PlannedBudget).HasPrecision(18, 2);
            e.Property(x => x.GeneralNotes).HasMaxLength(4000);
            e.HasIndex(x => new { x.UserId, x.StartDate }).HasDatabaseName("IX_TravelPlans_UserId_StartDate");
        });

        modelBuilder.Entity<TravelPlanShareLinkEntity>(e =>
        {
            e.ToTable("TravelPlanShareLinks");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasColumnType("varbinary(32)").IsRequired();
            e.Property(x => x.Permission).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();

            e.HasOne<TravelPlanEntity>()
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

            e.HasOne<TravelPlanEntity>()
                .WithMany()
                .HasForeignKey(x => x.TravelPlanId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
