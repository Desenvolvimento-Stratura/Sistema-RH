using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Entities;

namespace SistemaFerias.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Ferias> Ferias { get; set; }
    public DbSet<ExchangeAutoReplyBackup> ExchangeAutoReplyBackups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExchangeAutoReplyBackup>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Ferias)
                  .WithOne()
                  .HasForeignKey<ExchangeAutoReplyBackup>(e => e.FeriasId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.LoginAd).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExternalAudience).IsRequired().HasMaxLength(50);
        });
    }
}