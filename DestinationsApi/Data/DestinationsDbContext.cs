using DestinationsApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DestinationsApi.Data;

public sealed class DestinationsDbContext : DbContext
{
    public DestinationsDbContext(DbContextOptions<DestinationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelDestinationEntity> TravelDestinations => Set<TravelDestinationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        });
    }
}
