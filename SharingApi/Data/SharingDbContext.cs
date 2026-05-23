using Microsoft.EntityFrameworkCore;
using SharingApi.Data.Entities;

namespace SharingApi.Data;

public sealed class SharingDbContext : DbContext
{
    public SharingDbContext(DbContextOptions<SharingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelPlanRowEntity> TravelPlans => Set<TravelPlanRowEntity>();
    public DbSet<TravelPlanShareLinkEntity> TravelPlanShareLinks => Set<TravelPlanShareLinkEntity>();
    public DbSet<TravelPlanShareRecipientEntity> TravelPlanShareRecipients => Set<TravelPlanShareRecipientEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TravelPlanRowEntity>(e =>
        {
            e.ToTable("TravelPlans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(500).IsRequired();
            e.Property(x => x.StartDate).HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnType("date");
            e.Property(x => x.PlannedBudget).HasPrecision(18, 2);
            e.Property(x => x.GeneralNotes).HasMaxLength(4000);
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
                .OnDelete(DeleteBehavior.Cascade);
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
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
