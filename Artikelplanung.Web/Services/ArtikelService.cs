using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Data;
using Artikelplanung.Web.Models;

namespace Artikelplanung.Web.Services;

public class ArtikelService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task<List<ArtikelEintrag>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ArtikelEintraege
            .Include(a => a.ImportierteSpalten)
            .OrderByDescending(a => a.Prioritaet)
            .ThenBy(a => a.ReleaseDatum)
            .ThenByDescending(a => a.ErstelltAm)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetBekanntePersonenAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ArtikelEintraege
            .Select(a => a.EingetragenVon)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(ct);
    }

    public async Task<ArtikelEintrag> AddAsync(ArtikelEintrag eintrag, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        eintrag.ErstelltAm = DateTime.Now;
        db.ArtikelEintraege.Add(eintrag);
        await db.SaveChangesAsync(ct);
        return eintrag;
    }

    public async Task UpdateAsync(ArtikelEintrag eintrag, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ArtikelEintraege.Update(eintrag);
        db.Entry(eintrag).Property(a => a.ErstelltAm).IsModified = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.ArtikelEintraege.Where(a => a.Id == id).ExecuteDeleteAsync(ct);
    }
}
