<#
.SYNOPSIS
  Spielt eine neue Version der Artikelplanung auf dem Server ein (docs/deployment.md, Abschnitt "Update").

.DESCRIPTION
  Erwartet das ZIP aus "dotnet publish" in D:\Artikelplanung\staging (das neueste wird genommen,
  oder -Zip angeben). Entpackt nach staging\neu, stoppt den Dienst, verschiebt die laufende
  Version nach staging\vorher (Rückweg), setzt die neue Version nach app\ und startet den Dienst.
  Daten (D:\Artikelplanung\data) und Zertifikat (D:\Artikelplanung\cert) werden nicht angefasst.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File D:\Artikelplanung\deploy\Update-Server.ps1
#>
param(
    [string]$Zip,
    [string]$Root = 'D:\Artikelplanung',
    [string]$ServiceName = 'Artikelplanung'
)

$ErrorActionPreference = 'Stop'
$app     = Join-Path $Root 'app'
$staging = Join-Path $Root 'staging'
$neu     = Join-Path $staging 'neu'
$vorher  = Join-Path $staging 'vorher'

if (-not $Zip) {
    $Zip = Get-ChildItem (Join-Path $staging '*.zip') -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $Zip -or -not (Test-Path $Zip)) { throw "Kein ZIP gefunden. Erwartet in $staging (Ausgabe von dotnet publish, gepackt)." }

Write-Host "Entpacke $Zip ..."
if (Test-Path $neu) { Remove-Item $neu -Recurse -Force }
Expand-Archive -Path $Zip -DestinationPath $neu -Force
if (-not (Test-Path (Join-Path $neu 'Artikelplanung.Web.exe'))) { throw "Das ZIP enthält keine Artikelplanung.Web.exe – falsches Paket?" }

$svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -ne 'Stopped') {
    Write-Host "Stoppe Dienst $ServiceName ..."
    Stop-Service $ServiceName
    $svc.WaitForStatus('Stopped', (New-TimeSpan -Seconds 60))
}

if (Test-Path $vorher) { Remove-Item $vorher -Recurse -Force }
if (Test-Path $app) {
    Write-Host "Sichere laufende Version nach $vorher ..."
    Move-Item $app $vorher
}
Move-Item $neu $app

if ($svc) {
    Write-Host "Starte Dienst $ServiceName ..."
    Start-Service $ServiceName
    Start-Sleep -Seconds 5
    Get-Service $ServiceName | Format-Table Name, Status, StartType -AutoSize
    Write-Host "Fertig. Letzte Ereignisse:"
    Get-WinEvent -LogName Application -MaxEvents 10 -ErrorAction SilentlyContinue |
        Where-Object ProviderName -eq 'Artikelplanung.Web' |
        Format-Table TimeCreated, LevelDisplayName, @{ n = 'Message'; e = { $_.Message.Substring(0, [Math]::Min(120, $_.Message.Length)) } } -AutoSize
}
else {
    Write-Host "Dateien liegen in $app. Der Dienst ist noch nicht registriert – siehe docs/deployment.md, Abschnitt 5."
}
