namespace HorusAfiliadosExtractor.App.Services;

public sealed class ExtractionControl : IDisposable
{
    private readonly ManualResetEventSlim _resume = new(initialState: true);

    public bool IsPaused { get; private set; }

    public event Action? StateChanged;

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        _resume.Reset();
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        _resume.Set();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Bloquea si está en pausa hasta que se llame Resume o se cancele el token.
    /// </summary>
    public Task WaitIfPausedAsync(CancellationToken ct)
    {
        if (!IsPaused) return Task.CompletedTask;
        return Task.Run(() => _resume.Wait(ct), ct);
    }

    public void Dispose() => _resume.Dispose();
}
