namespace HorusAfiliadosExtractor.App.Utils;

public sealed class Logger : IDisposable
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;
    private readonly Action<string>? _uiSink;

    public Logger(string logDir, Action<string>? uiSink = null)
    {
        Directory.CreateDirectory(logDir);
        var path = Path.Combine(logDir, $"horus_extractor_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        _writer = new StreamWriter(path, append: false) { AutoFlush = true };
        _uiSink = uiSink;
        Info($"Log: {path}");
    }

    public void Info(string message) => Write("INF", message);
    public void Warn(string message) => Write("WRN", message);
    public void Error(string message) => Write("ERR", message);

    public void Error(Exception ex, string message)
    {
        Write("ERR", $"{message} | {ex.GetType().Name}: {ex.Message}");
        Write("ERR", ex.StackTrace ?? string.Empty);
    }

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss} {level}] {message}";
        lock (_lock)
        {
            try { _writer.WriteLine(line); } catch { /* no bloquear por IO */ }
        }
        try { _uiSink?.Invoke(line); } catch { /* la UI no debe tumbar el bot */ }
    }

    public void Dispose()
    {
        try { _writer.Dispose(); } catch { /* ignore */ }
    }
}
