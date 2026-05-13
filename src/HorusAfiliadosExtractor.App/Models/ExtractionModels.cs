namespace HorusAfiliadosExtractor.App.Models;

public sealed class InputRecord
{
    public int RowNumber { get; set; }
    public string Documento { get; set; } = string.Empty;
}

public sealed class ExtractionResult
{
    public string DocumentoConsultado { get; set; } = string.Empty;
    public DateTime FechaExtraccion { get; set; } = DateTime.Now;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Seconds { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public string ScreenshotPath { get; set; } = string.Empty;
    public List<FieldValue> Fields { get; set; } = new();
    public List<TableCellValue> TableCells { get; set; } = new();
    public List<BodyTextValue> BodyTexts { get; set; } = new();
}

public sealed class FieldValue
{
    public string DocumentoConsultado { get; set; } = string.Empty;
    public DateTime FechaExtraccion { get; set; }
    public string Tab { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Campo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public sealed class TableCellValue
{
    public string DocumentoConsultado { get; set; } = string.Empty;
    public DateTime FechaExtraccion { get; set; }
    public string Tab { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public sealed class BodyTextValue
{
    public string DocumentoConsultado { get; set; } = string.Empty;
    public DateTime FechaExtraccion { get; set; }
    public string Tab { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class DomExtractionPayload
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TabName { get; set; } = string.Empty;
    public List<DomField> Fields { get; set; } = new();
    public List<DomTable> Tables { get; set; } = new();
    public string BodyText { get; set; } = string.Empty;
}

public sealed class DomField
{
    public string Section { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public sealed class DomTable
{
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}
