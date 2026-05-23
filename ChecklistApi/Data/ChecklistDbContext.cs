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
    public DbSet<TravelPlanShareLinkEntity> TravelPlanShareLinks => Set<TravelPlanShareLinkEntity>();
    public DbSet<TravelPlanShareRecipientEntity> TravelPlanShareRecipients => Set<TravelPlanShareRecipientEntity>();

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
