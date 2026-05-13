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
        cfg.Bot.ProfileDir = ToFullPath(cfg.Bot.ProfileDir, configDir);
        cfg.Bot.InputPath = ToFullPath(cfg.Bot.InputPath, configDir);
        cfg.Bot.OutputExcelPath = ToFullPath(cfg.Bot.OutputExcelPath, configDir);
        cfg.Bot.EvidenceDir = ToFullPath(cfg.Bot.EvidenceDir, configDir);
        cfg.Bot.LogDir = ToFullPath(cfg.Bot.LogDir, configDir);

        Directory.CreateDirectory(cfg.Bot.ProfileDir);
        Directory.CreateDirectory(Path.GetDirectoryName(cfg.Bot.OutputExcelPath) ?? ".");
        Directory.CreateDirectory(cfg.Bot.EvidenceDir);
        Directory.CreateDirectory(cfg.Bot.LogDir);
    }

    private static string ToFullPath(string path, string baseDir)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseDir;
        if (Path.IsPathRooted(path)) return path;

        // appsettings está en /config; las rutas relativas deben apuntar a la raíz del proyecto.
        var root = Directory.GetParent(baseDir)?.FullName ?? baseDir;
        return Path.GetFullPath(Path.Combine(root, path));
    }
}
