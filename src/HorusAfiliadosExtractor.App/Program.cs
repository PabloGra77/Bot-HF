using System.Windows.Forms;
using HorusAfiliadosExtractor.App.Forms;
using HorusAfiliadosExtractor.App.Models;
using HorusAfiliadosExtractor.App.Services;
using HorusAfiliadosExtractor.App.Utils;
using Velopack;

namespace HorusAfiliadosExtractor.App;

internal static class Program
{
    public static UpdateService Updater { get; } = new();

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        ApplicationConfiguration.Initialize();

        // Chequeo de actualizaciones en background: no bloquea la UI.
        _ = Task.Run(async () => await Updater.CheckAndDownloadAsync());

        try
        {
            var configPath = ResolveConfigPath(args);
            var cfg = ConfigLoader.Load(configPath);

            using var login = new LoginForm(cfg.Bot.InputPath);
            if (login.ShowDialog() != DialogResult.OK)
            {
                Updater.ApplyPendingIfAnyOnExit();
                return;
            }

            cfg.Bot.InputPath = login.InputPath;

            var progress = new ProgressForm();
            progress.Show();

            void OnUpdateStatus(string m) { try { progress.AppendLog($"[UPDATE] {m}"); } catch { } }
            void OnUpdateReady(string v) { try { progress.AppendLog($"[UPDATE] Versión {v} lista. Se aplicará al cerrar."); } catch { } }
            Updater.StatusChanged += OnUpdateStatus;
            Updater.UpdateReady += OnUpdateReady;

            var cts = new CancellationTokenSource();
            var control = new ExtractionControl();

            progress.PauseRequested += () =>
            {
                control.Pause();
                progress.AppendLog("[CTRL] Detener solicitado. El bot terminará el documento actual y guardará el Excel con todo lo procesado.");
                progress.SetPausedState();
            };
            progress.ResumeRequested += () =>
            {
                control.Resume();
                progress.AppendLog("[CTRL] Continuar solicitado. Reanudando extracción...");
                progress.SetResumedState();
            };
            progress.FinalizeRequested += () =>
            {
                progress.AppendLog("[CTRL] Finalizar solicitado. Guardando Excel y cerrando proceso...");
                control.Resume(); // si estaba pausado, liberar para que el bot pueda observar la cancelación
                if (!cts.IsCancellationRequested) cts.Cancel();
            };

            progress.FormClosing += (_, _) =>
            {
                if (!cts.IsCancellationRequested) cts.Cancel();
                control.Resume();
                Updater.StatusChanged -= OnUpdateStatus;
                Updater.UpdateReady -= OnUpdateReady;
            };

            Task.Run(() => RunExtractionAsync(cfg, login.Credentials, progress, control, cts.Token));

            Application.Run(progress);
            control.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error iniciando la aplicación:\n\n{ex.Message}", "Bot HF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Updater.ApplyPendingIfAnyOnExit();
        }
    }

    private static async Task RunExtractionAsync(AppConfig cfg, LoginCredentials credentials, ProgressForm progress, ExtractionControl control, CancellationToken ct)
    {
        Logger? log = null;
        var success = false;
        try
        {
            log = new Logger(cfg.Bot.LogDir, progress.AppendLog);
            log.Info("HORUS FPS - Bot HF (Extractor de Afiliados)");
            log.Info($"Entrada: {cfg.Bot.InputPath}");
            log.Info($"Salida:  {cfg.Bot.OutputExcelPath}");
            log.Info($"Tipo de usuario: {credentials.Type}");

            var records = InputReader.Read(cfg.Bot.InputPath, cfg.Bot.DocumentoHeader);
            if (records.Count == 0)
            {
                log.Warn("No hay documentos para procesar en el archivo de entrada.");
                progress.Finish(cfg.Bot.OutputExcelPath, false);
                return;
            }

            log.Info($"Documentos leídos: {records.Count}");
            progress.SetTotal(records.Count);

            var bot = new HorusExtractorBot(
                cfg.Bot,
                log,
                credentials,
                (current, total, status) => progress.SetProgress(current, total, status),
                control);

            var results = await bot.RunAsync(records, ct);

            try
            {
                ExcelWriter.Save(cfg.Bot.OutputExcelPath, results);
                log.Info($"Excel final generado: {cfg.Bot.OutputExcelPath}");
            }
            catch (Exception ex)
            {
                log.Error(ex, "No se pudo guardar el Excel final. Si está abierto, ciérrelo.");
            }

            log.Info($"OK: {results.Count(r => r.Success)} | ERROR: {results.Count(r => !r.Success)}");
            success = results.Any() && results.All(r => r.Success);
        }
        catch (OperationCanceledException)
        {
            log?.Warn("Proceso cancelado por el usuario.");
        }
        catch (Exception ex)
        {
            log?.Error(ex, "Error general en la extracción.");
        }
        finally
        {
            progress.Finish(cfg.Bot.OutputExcelPath, success);
            log?.Dispose();
        }
    }

    private static string ResolveConfigPath(string[] args)
    {
        var idx = Array.FindIndex(args, a => string.Equals(a, "--config", StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && idx + 1 < args.Length)
            return Path.GetFullPath(args[idx + 1]);

        var baseDir = AppContext.BaseDirectory;

        var nearExe = Path.Combine(baseDir, "config", "appsettings.json");
        if (File.Exists(nearExe)) return nearExe;

        var rootConfig = Path.Combine(Directory.GetCurrentDirectory(), "config", "appsettings.json");
        if (File.Exists(rootConfig)) return rootConfig;

        var localConfig = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        if (File.Exists(localConfig)) return localConfig;

        return Path.GetFullPath("config/appsettings.json");
    }
}
