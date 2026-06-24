using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Entities;

namespace SistemaFerias.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ferias> Ferias { get; set; }
}