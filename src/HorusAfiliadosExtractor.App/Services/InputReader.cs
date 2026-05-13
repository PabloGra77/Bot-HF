using ClosedXML.Excel;
using HorusAfiliadosExtractor.App.Models;

namespace HorusAfiliadosExtractor.App.Services;

public static class InputReader
{
    public static List<InputRecord> Read(string path, string documentoHeader)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No existe el archivo de entrada: {path}");

        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".xlsx" or ".xlsm" => ReadExcel(path, documentoHeader),
            ".csv" or ".txt" => ReadCsv(path, documentoHeader),
            _ => throw new InvalidOperationException($"Extensión de entrada no soportada: {ext}. Use .xlsx o .csv")
        };
    }

    private static List<InputRecord> ReadExcel(string path, string documentoHeader)
    {
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheets.First();
        var used = ws.RangeUsed() ?? throw new InvalidOperationException("El archivo Excel de entrada está vacío.");

        var headerRow = used.FirstRowUsed();
        var col = headerRow.Cells().FirstOrDefault(c => Same(c.GetString(), documentoHeader))?.Address.ColumnNumber;
        if (col is null)
            throw new InvalidOperationException($"No se encontró la columna '{documentoHeader}' en el Excel.");

        var records = new List<InputRecord>();
        foreach (var row in used.RowsUsed().Skip(1))
        {
            var doc = OnlyDigits(row.Cell(col.Value).GetString());
            if (!string.IsNullOrWhiteSpace(doc))
                records.Add(new InputRecord { RowNumber = row.RowNumber(), Documento = doc });
        }

        return ValidateAndDistinct(records, path);
    }

    private static List<InputRecord> ReadCsv(string path, string documentoHeader)
    {
        var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count == 0) throw new InvalidOperationException("El CSV de entrada está vacío.");

        var sep = lines[0].Contains(';') ? ';' : ',';
        var headers = SplitCsvLine(lines[0], sep);
        var idx = headers.FindIndex(h => Same(h, documentoHeader));
        if (idx < 0) throw new InvalidOperationException($"No se encontró la columna '{documentoHeader}' en el CSV.");

        var records = new List<InputRecord>();
        for (var i = 1; i < lines.Count; i++)
        {
            var parts = SplitCsvLine(lines[i], sep);
            if (idx >= parts.Count) continue;

            var doc = OnlyDigits(parts[idx]);
            if (!string.IsNullOrWhiteSpace(doc))
                records.Add(new InputRecord { RowNumber = i + 1, Documento = doc });
        }

        return ValidateAndDistinct(records, path);
    }

    private static List<InputRecord> ValidateAndDistinct(List<InputRecord> records, string path)
    {
        var clean = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Documento))
            .DistinctBy(r => r.Documento)
            .ToList();

        if (clean.Count == 1 && IsSampleDocument(clean[0].Documento))
        {
            throw new InvalidOperationException(
                $"El archivo de entrada todavía contiene el documento de ejemplo '{clean[0].Documento}'. " +
                $"Abra este archivo y reemplace el ejemplo por las cédulas reales: {path}");
        }

        return clean;
    }

    private static bool IsSampleDocument(string doc)
    {
        var value = OnlyDigits(doc);
        return value is "1234567890" or "0000000000" or "1111111111" or "9999999999";
    }

    private static List<string> SplitCsvLine(string line, char sep)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (ch == sep && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }
            current.Append(ch);
        }
        result.Add(current.ToString().Trim());
        return result;
    }

    private static bool Same(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value)
    {
        value = value.Trim();
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());
}
