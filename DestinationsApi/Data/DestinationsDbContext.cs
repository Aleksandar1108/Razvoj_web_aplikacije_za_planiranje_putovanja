using Microsoft.EntityFrameworkCore;
using DestinationsApi.Data.Entities;

namespace DestinationsApi.Data;

public sealed class DestinationsDbContext : DbContext
{
    public DestinationsDbContext(DbContextOptions<DestinationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelDestinationEntity> TravelDestinations => Set<TravelDestinationEntity>();
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

        modelBuilder.Entity<TravelDestinationEntity>(e =>
        {
            e.ToTable("TravelDestinations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Location).HasMaxLength(300).IsRequired();
            e.Property(x => x.ArrivalDate).HasColumnType("date");
            e.Property(x => x.DepartureDate).HasColumnType("date");
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.TravelPlanId, x.ArrivalDate }).HasDatabaseName("IX_TravelDestinations_TravelPlanId_Arrival");

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
