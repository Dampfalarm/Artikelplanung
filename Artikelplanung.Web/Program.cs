using Microsoft.EntityFrameworkCore;
using Artikelplanung.Web.Components;
using Artikelplanung.Web.Data;
using Artikelplanung.Web.Services;
using Artikelplanung.Web.Services.Import;

try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch (IOException) { /* z. B. bei umgeleiteter Ausgabe ohne Konsole */ }

var builder = WebApplication.CreateBuilder(args);

// Läuft lokal als normale Konsolen-App, kann als Windows-Dienst registriert werden (gleiches
// Muster wie die Rechnungsablage, docs dort für den Rollout-Ablauf).
builder.Host.UseWindowsService();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Datenverzeichnis: lokal <Projekt>/Data, sonst über "DataDirectory" konfigurierbar.
var dataPaths = DataPaths.Resolve(builder.Configuration, builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataPaths.Directory);
builder.Services.AddSingleton(dataPaths);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? $"DataSource={dataPaths.Database};Cache=Shared";
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ArtikelService>();
builder.Services.AddScoped<ArtikelImportService>();
builder.Services.AddScoped<AktivePersonState>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // WAL-Modus, damit gleichzeitige Zugriffe (z. B. Import + Liste offen) sich nicht blockieren.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
