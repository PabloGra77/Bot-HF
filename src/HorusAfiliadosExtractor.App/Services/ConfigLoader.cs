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
        // Datos del usuario fuera del install dir (Velopack rota 'current\' en cada update).
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var botData = Path.Combine(localAppData, "BotHFData");

        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var botDocs = Path.Combine(docs, "BotHF");

        cfg.Bot.ProfileDir = ResolveDataPath(cfg.Bot.ProfileDir, botData, "profiles/horus_fps");
        cfg.Bot.EvidenceDir = ResolveDataPath(cfg.Bot.EvidenceDir, botDocs, "evidence");
        cfg.Bot.LogDir = ResolveDataPath(cfg.Bot.LogDir, botDocs, "logs");
        cfg.Bot.OutputExcelPath = ResolveDataPath(cfg.Bot.OutputExcelPath, botDocs, "output/extraccion_horus_afiliados.xlsx");

        // El archivo de entrada se sigue resolviendo contra la raíz del proyecto en dev,
        // pero la UI permite que el usuario lo escoja con un OpenFileDialog.
        cfg.Bot.InputPath = ToProjectPath(cfg.Bot.InputPath, configDir);

        Directory.CreateDirectory(cfg.Bot.ProfileDir);
        Directory.CreateDirectory(Path.GetDirectoryName(cfg.Bot.OutputExcelPath) ?? botDocs);
        Directory.CreateDirectory(cfg.Bot.EvidenceDir);
        Directory.CreateDirectory(cfg.Bot.LogDir);
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
