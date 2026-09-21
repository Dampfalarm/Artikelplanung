using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Models;

namespace Artikelplanung.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ArtikelEintrag> ArtikelEintraege => Set<ArtikelEintrag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ArtikelEintrag>(e =>
        {
            e.HasIndex(a => a.Ean);
        });
    }
}
