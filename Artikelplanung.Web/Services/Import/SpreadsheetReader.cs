using ClosedXML.Excel;

namespace Artikelplanung.Web.Services.Import;

/// <summary>Liest das erste Tabellenblatt einer .xlsx-Datei als flache Tabelle ein (siehe
/// SpreadsheetTable). Reine Parsing-Logik ohne Datenbankzugriff, damit sie sich ohne SQLite
/// testen lässt.</summary>
public static class SpreadsheetReader
{
    public static SpreadsheetTable Read(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var sheet = workbook.Worksheets.First();
        var usedRange = sheet.RangeUsed()
            ?? throw new InvalidDataException("Das Tabellenblatt enthält keine Daten.");

        var rows = usedRange.RowsUsed().ToList();
        if (rows.Count == 0)
        {
            throw new InvalidDataException("Das Tabellenblatt enthält keine Daten.");
        }

        var headerRow = rows[0];
        var columnCount = usedRange.ColumnCount();
        var headers = new List<string>(columnCount);
        var seen = new Dictionary<string, int>();
        for (var col = 1; col <= columnCount; col++)
        {
            var raw = headerRow.Cell(col).GetString().Trim();
            var name = string.IsNullOrEmpty(raw) ? $"Spalte {col}" : raw;
            if (seen.TryGetValue(name, out var count))
            {
                seen[name] = count + 1;
                name = $"{name} ({count + 1})";
            }
            else
            {
                seen[name] = 1;
            }
            headers.Add(name);
        }

        var dataRows = new List<IReadOnlyList<string?>>();
        foreach (var row in rows.Skip(1))
        {
            var values = new List<string?>(columnCount);
            var anyValue = false;
            for (var col = 1; col <= columnCount; col++)
            {
                var value = row.Cell(col).GetString().Trim();
                if (value.Length > 0) anyValue = true;
                values.Add(value.Length == 0 ? null : value);
            }
            if (anyValue) dataRows.Add(values);
        }

        return new SpreadsheetTable { Headers = headers, Rows = dataRows };
    }
}
