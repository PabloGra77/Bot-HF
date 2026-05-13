using ClosedXML.Excel;
using HorusAfiliadosExtractor.App.Models;

namespace HorusAfiliadosExtractor.App.Services;

public static class ExcelWriter
{
    public static void Save(string path, IReadOnlyList<ExtractionResult> results)
    {
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(dir)) dir = ".";
        Directory.CreateDirectory(dir);

        var fileName = Path.GetFileNameWithoutExtension(path);
        var tempPath = Path.Combine(dir, $"{fileName}.tmp_{Guid.NewGuid():N}.xlsx");

        try
        {
            using (var wb = new XLWorkbook())
            {
                WriteLogSheet(wb, results);
                WriteLongFieldsSheet(wb, results.SelectMany(r => r.Fields).ToList());
                WriteSummarySheet(wb, results);
                WriteTablesSheet(wb, results.SelectMany(r => r.TableCells).ToList());
                WriteBodyTextSheet(wb, results.SelectMany(r => r.BodyTexts).ToList());

                wb.SaveAs(tempPath);
            }

            File.Copy(tempPath, path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch
            {
                // No bloquear el proceso por residuos temporales.
            }
        }
    }

    private static void WriteLogSheet(XLWorkbook wb, IReadOnlyList<ExtractionResult> results)
    {
        var ws = wb.Worksheets.Add("LOG_PROCESO");
        var headers = new[] { "FechaExtraccion", "DocumentoConsultado", "Estado", "Mensaje", "Segundos", "Url", "Screenshot" };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var r in results)
        {
            ws.Cell(row, 1).Value = r.FechaExtraccion;
            ws.Cell(row, 2).Value = r.DocumentoConsultado;
            ws.Cell(row, 3).Value = r.Success ? "OK" : "ERROR";
            ws.Cell(row, 4).Value = r.Message;
            ws.Cell(row, 5).Value = r.Seconds;
            ws.Cell(row, 6).Value = r.PageUrl;
            ws.Cell(row, 7).Value = r.ScreenshotPath;
            row++;
        }

        FormatSheet(ws, headers.Length, Math.Max(1, results.Count));
    }

    private static void WriteLongFieldsSheet(XLWorkbook wb, IReadOnlyList<FieldValue> rows)
    {
        var ws = wb.Worksheets.Add("DATOS_LARGOS");
        var headers = new[] { "FechaExtraccion", "DocumentoConsultado", "Pestaña", "Seccion", "Campo", "Valor", "Fuente" };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var item in rows)
        {
            ws.Cell(row, 1).Value = item.FechaExtraccion;
            ws.Cell(row, 2).Value = item.DocumentoConsultado;
            ws.Cell(row, 3).Value = item.Tab;
            ws.Cell(row, 4).Value = item.Section;
            ws.Cell(row, 5).Value = item.Campo;
            ws.Cell(row, 6).Value = item.Valor;
            ws.Cell(row, 7).Value = item.Source;
            row++;
        }

        FormatSheet(ws, headers.Length, Math.Max(1, rows.Count));
        ws.Column(6).Width = 55;
        ws.Column(6).Style.Alignment.WrapText = true;
    }

    private static void WriteSummarySheet(XLWorkbook wb, IReadOnlyList<ExtractionResult> results)
    {
        var ws = wb.Worksheets.Add("RESUMEN_AFILIADOS");

        var preferred = new[]
        {
            "Tipo documento", "Documento", "Nombre completo", "Primer nombre", "Segundo nombre", "Primer apellido", "Segundo apellido",
            "Tipo afiliado", "Estado", "Parentesco", "Edad", "Fecha nacimiento", "Sexo biológico", "Sexo identificación",
            "Número identificación cotizante principal/Causante", "Codigo Parentesco"
        };

        var dynamicFields = results
            .SelectMany(r => r.Fields)
            .Where(f => !string.IsNullOrWhiteSpace(f.Campo))
            .Select(f => f.Campo.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var columns = new List<string> { "FechaExtraccion", "DocumentoConsultado", "EstadoBot", "Mensaje" };
        columns.AddRange(preferred.Where(p => dynamicFields.Contains(p, StringComparer.OrdinalIgnoreCase)));
        columns.AddRange(dynamicFields.Where(f => !columns.Contains(f, StringComparer.OrdinalIgnoreCase)).Take(180));
        WriteHeader(ws, columns.ToArray());

        var row = 2;
        foreach (var r in results)
        {
            var dict = r.Fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Campo) && !string.IsNullOrWhiteSpace(f.Valor))
                .GroupBy(f => f.Campo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => string.Join(" | ", g.Select(x => x.Valor).Distinct().Take(3)), StringComparer.OrdinalIgnoreCase);

            ws.Cell(row, 1).Value = r.FechaExtraccion;
            ws.Cell(row, 2).Value = r.DocumentoConsultado;
            ws.Cell(row, 3).Value = r.Success ? "OK" : "ERROR";
            ws.Cell(row, 4).Value = r.Message;

            for (var c = 5; c <= columns.Count; c++)
            {
                var key = columns[c - 1];
                if (dict.TryGetValue(key, out var value))
                    ws.Cell(row, c).Value = value;
            }
            row++;
        }

        FormatSheet(ws, columns.Count, Math.Max(1, results.Count));
        ws.Column(4).Width = 45;
        ws.Column(4).Style.Alignment.WrapText = true;
    }

    private static void WriteTablesSheet(XLWorkbook wb, IReadOnlyList<TableCellValue> rows)
    {
        var ws = wb.Worksheets.Add("TABLAS");
        var headers = new[] { "FechaExtraccion", "DocumentoConsultado", "Pestaña", "Tabla", "Fila", "Columna", "Valor" };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var item in rows)
        {
            ws.Cell(row, 1).Value = item.FechaExtraccion;
            ws.Cell(row, 2).Value = item.DocumentoConsultado;
            ws.Cell(row, 3).Value = item.Tab;
            ws.Cell(row, 4).Value = item.TableName;
            ws.Cell(row, 5).Value = item.RowIndex;
            ws.Cell(row, 6).Value = item.ColumnName;
            ws.Cell(row, 7).Value = item.Valor;
            row++;
        }

        FormatSheet(ws, headers.Length, Math.Max(1, rows.Count));
        ws.Column(7).Width = 60;
        ws.Column(7).Style.Alignment.WrapText = true;
    }

    private static void WriteBodyTextSheet(XLWorkbook wb, IReadOnlyList<BodyTextValue> rows)
    {
        var ws = wb.Worksheets.Add("TEXTO_VISIBLE");
        var headers = new[] { "FechaExtraccion", "DocumentoConsultado", "Pestaña", "TextoVisible" };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var item in rows)
        {
            ws.Cell(row, 1).Value = item.FechaExtraccion;
            ws.Cell(row, 2).Value = item.DocumentoConsultado;
            ws.Cell(row, 3).Value = item.Tab;
            ws.Cell(row, 4).Value = item.Text;
            row++;
        }

        FormatSheet(ws, headers.Length, Math.Max(1, rows.Count));
        ws.Column(4).Width = 100;
        ws.Column(4).Style.Alignment.WrapText = true;
    }

    private static void WriteHeader(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
            ws.Cell(1, i + 1).Value = headers[i];
    }

    private static void FormatSheet(IXLWorksheet ws, int columns, int dataRows)
    {
        var used = ws.Range(1, 1, Math.Max(1, dataRows + 1), columns);
        var header = ws.Range(1, 1, 1, columns);

        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        used.SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, columns).AdjustToContents(1, Math.Min(dataRows + 1, 300));

        foreach (var col in ws.Columns(1, columns))
        {
            if (col.Width > 55) col.Width = 55;
            if (col.Width < 11) col.Width = 11;
        }

        ws.Column(1).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
    }
}
