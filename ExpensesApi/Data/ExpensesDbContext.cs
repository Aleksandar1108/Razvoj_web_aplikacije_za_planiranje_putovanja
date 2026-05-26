using ExpensesApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpensesApi.Data;

public sealed class ExpensesDbContext : DbContext
{
    public ExpensesDbContext(DbContextOptions<ExpensesDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelExpenseEntity> TravelExpenses => Set<TravelExpenseEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TravelExpenseEntity>(e =>
        {
            e.ToTable("TravelExpenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.ExpenseDate).HasColumnType("date");
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => new { x.TravelPlanId, x.ExpenseDate }).HasDatabaseName("IX_TravelExpenses_TravelPlanId_Date");
        });
    }
}
