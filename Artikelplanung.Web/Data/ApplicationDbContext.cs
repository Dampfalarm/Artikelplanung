using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Models;

namespace Artikelplanung.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ArtikelEintrag> ArtikelEintraege => Set<ArtikelEintrag>();
    public DbSet<ImportierteSpalte> ImportierteSpalten => Set<ImportierteSpalte>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ArtikelEintrag>(e =>
        {
            e.HasIndex(a => a.Ean);
            e.HasMany(a => a.ImportierteSpalten)
                .WithOne()
                .HasForeignKey(s => s.ArtikelEintragId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
