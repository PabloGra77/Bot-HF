using System.Windows.Forms;
using HorusAfiliadosExtractor.App.Forms;
using HorusAfiliadosExtractor.App.Models;
using HorusAfiliadosExtractor.App.Services;
using HorusAfiliadosExtractor.App.Utils;

namespace HorusAfiliadosExtractor.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var configPath = ResolveConfigPath(args);
            var cfg = ConfigLoader.Load(configPath);

            using var login = new LoginForm(cfg.Bot.InputPath);
            if (login.ShowDialog() != DialogResult.OK) return;

            cfg.Bot.InputPath = login.InputPath;

            var progress = new ProgressForm();
            progress.Show();

            var cts = new CancellationTokenSource();
            progress.FormClosing += (_, e) =>
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }
            };

            Task.Run(() => RunExtractionAsync(cfg, login.Credentials, progress, cts.Token));

            Application.Run(progress);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error iniciando la aplicación:\n\n{ex.Message}", "Bot HF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static async Task RunExtractionAsync(AppConfig cfg, LoginCredentials credentials, ProgressForm progress, CancellationToken ct)
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
                (current, total, status) => progress.SetProgress(current, total, status));

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
