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
            .OrderByDescending(a => a.Prioritaet)
            .ThenBy(a => a.ReleaseDatum == null)
            .ThenBy(a => a.ReleaseDatum)
            .ThenByDescending(a => a.ErstelltAm)
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

    public async Task SetArchiviertAsync(int id, bool archiviert, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.ArtikelEintraege
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Archiviert, archiviert), ct);
    }

    /// <summary>Sammelaktion: setzt alle nicht archivierten Einträge mit Status "Angelegt" auf
    /// archiviert. Gibt die Anzahl der geänderten Einträge zurück.</summary>
    public async Task<int> ArchiviereAngelegteAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ArtikelEintraege
            .Where(a => !a.Archiviert && a.Status == ArtikelStatus.Angelegt)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Archiviert, true), ct);
    }
}
