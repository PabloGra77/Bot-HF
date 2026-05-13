using System.Text.Json;
using HorusAfiliadosExtractor.App.Models;

namespace HorusAfiliadosExtractor.App.Services;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No existe el archivo de configuración: {path}");

        var json = File.ReadAllText(path);
        var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        Normalize(cfg, Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory());
        return cfg;
    }

    private static void Normalize(AppConfig cfg, string configDir)
    {
        // Perfil persistente del navegador: en %LOCALAPPDATA% para sobrevivir updates de Velopack.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var profileBase = Path.Combine(localAppData, "BotHFData");

        // Resultados visibles para el usuario: en Descargas\BotHF.
        var downloads = ResolveDownloadsFolder();
        var botDownloads = Path.Combine(downloads, "BotHF");

        cfg.Bot.ProfileDir = ResolveDataPath(cfg.Bot.ProfileDir, profileBase, "profiles/horus_fps");
        cfg.Bot.EvidenceDir = ResolveDataPath(cfg.Bot.EvidenceDir, botDownloads, "Pantallazos");
        cfg.Bot.LogDir = ResolveDataPath(cfg.Bot.LogDir, botDownloads, "Logs");
        cfg.Bot.OutputExcelPath = ResolveDataPath(cfg.Bot.OutputExcelPath, botDownloads, "Excel/extraccion_horus_afiliados.xlsx");

        cfg.Bot.InputPath = ToProjectPath(cfg.Bot.InputPath, configDir);

        Directory.CreateDirectory(cfg.Bot.ProfileDir);
        Directory.CreateDirectory(Path.GetDirectoryName(cfg.Bot.OutputExcelPath) ?? botDownloads);
        Directory.CreateDirectory(cfg.Bot.EvidenceDir);
        Directory.CreateDirectory(cfg.Bot.LogDir);
    }

    private static string ResolveDownloadsFolder()
    {
        // Windows: %USERPROFILE%\Downloads. La constante FOLDERID_Downloads no existe
        // en Environment.SpecialFolder, pero esta convención cubre el caso por defecto.
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = Path.Combine(profile, "Downloads");
        if (Directory.Exists(downloads)) return downloads;

        // Si el usuario redirigió Downloads, intentar leer del registro.
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            var value = key?.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string;
            if (!string.IsNullOrWhiteSpace(value))
                return Environment.ExpandEnvironmentVariables(value);
        }
        catch { }

        Directory.CreateDirectory(downloads);
        return downloads;
    }

    private static string ResolveDataPath(string configValue, string baseDir, string fallback)
    {
        if (string.IsNullOrWhiteSpace(configValue)) configValue = fallback;
        if (Path.IsPathRooted(configValue)) return Path.GetFullPath(configValue);
        return Path.GetFullPath(Path.Combine(baseDir, configValue));
    }

    private static string ToProjectPath(string path, string baseDir)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseDir;
        if (Path.IsPathRooted(path)) return path;
        var root = Directory.GetParent(baseDir)?.FullName ?? baseDir;
        return Path.GetFullPath(Path.Combine(root, path));
    }
}
