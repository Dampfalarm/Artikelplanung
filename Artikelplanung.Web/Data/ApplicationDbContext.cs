using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Models;

namespace Artikelplanung.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ArtikelEintrag> ArtikelEintraege => Set<ArtikelEintrag>();
}
