using Microsoft.EntityFrameworkCore;
using EmployeeWeb.Api.Models.Entities;

namespace EmployeeWeb.Api.Data;

/// <summary>
/// Application database context. Uses SQLite for development; can swap to SQL Server for production.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<EmployeeEntity> Employees { get; set; }
    public DbSet<LoginLogEntity> LoginLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<EmployeeEntity>(e =>
        {
            e.HasIndex(x => x.StaffEmail).IsUnique();
            e.HasIndex(x => x.StaffID).IsUnique();
        });

        builder.Entity<LoginLogEntity>(e =>
        {
            e.HasIndex(x => new { x.EmployeeId, x.Date });
            e.HasOne(x => x.Employee)
                .WithMany(x => x.LoginLogs)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
