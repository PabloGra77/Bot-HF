using Velopack;
using Velopack.Sources;

namespace HorusAfiliadosExtractor.App.Services;

public sealed class UpdateService
{
    private const string GithubRepoUrl = "https://github.com/PabloGra77/Bot-HF";

    private readonly UpdateManager _manager;
    private UpdateInfo? _pending;

    public event Action<string>? UpdateReady;
    public event Action<string>? StatusChanged;

    public bool HasPendingUpdate => _pending != null;
    public string? PendingVersion => _pending?.TargetFullRelease.Version.ToString();

    public UpdateService()
    {
        _manager = new UpdateManager(
            new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false));
    }

    public async Task CheckAndDownloadAsync(CancellationToken ct = default)
    {
        if (!_manager.IsInstalled)
        {
            StatusChanged?.Invoke("Modo desarrollo: auto-update deshabilitado.");
            return;
        }

        try
        {
            StatusChanged?.Invoke("Buscando actualizaciones...");
            var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info == null)
            {
                StatusChanged?.Invoke("La aplicación está actualizada.");
                return;
            }

            var version = info.TargetFullRelease.Version.ToString();
            StatusChanged?.Invoke($"Descargando actualización {version}...");

            await _manager.DownloadUpdatesAsync(info, cancelToken: ct).ConfigureAwait(false);

            _pending = info;
            UpdateReady?.Invoke(version);
            StatusChanged?.Invoke($"Actualización {version} lista. Se aplicará al cerrar.");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"No se pudo verificar actualizaciones: {ex.Message}");
        }
    }

    public void ApplyPendingIfAnyOnExit()
    {
        if (_pending == null) return;
        try
        {
            _manager.WaitExitThenApplyUpdates(_pending, silent: true, restart: true);
        }
        catch
        {
            // Si no se puede aplicar el update por permisos o IO, no bloquear el cierre.
        }
    }
}
