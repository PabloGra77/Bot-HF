namespace HorusAfiliadosExtractor.App.Models;

public sealed class AppConfig
{
    public BotSettings Bot { get; set; } = new();
}

public sealed class BotSettings
{
    public string LoginUrl { get; set; } = "https://fps.horus-health.com/";
    public string ModuleUrl { get; set; } = "https://fps.horus-health.com/aseguramiento/afiliados";
    public string BrowserChannel { get; set; } = "msedge";
    public bool Headless { get; set; } = false;
    public int SlowMoMilliseconds { get; set; } = 40;
    public int TimeoutSeconds { get; set; } = 45;
    public int WaitAfterSearchMilliseconds { get; set; } = 1800;
    public int WaitAfterTabClickMilliseconds { get; set; } = 900;
    public bool ManualLogin { get; set; } = false;
    public bool PauseBeforeProcessing { get; set; } = false;
    public bool NavigateModuleAfterLogin { get; set; } = true;

    public string EmailFieldSelectors { get; set; } = "input[type='email'], input[autocomplete='email'], input[autocomplete='username'], input[name*='mail' i], input[name*='user' i], input[name*='usuario' i], input[id*='mail' i], input[id*='user' i], input[id*='usuario' i]";
    public string PasswordFieldSelectors { get; set; } = "input[type='password']";
    public string SubmitButtonSelectors { get; set; } = "button:has-text('INICIAR SESIÓN'), button:has-text('INICIAR SESION'), button:has-text('Iniciar sesión'), button:has-text('Iniciar sesion'), button[type='submit'], input[type='submit'], .v-btn:has-text('INICIAR')";
    public bool ClickNuevaConsultaBeforeEach { get; set; } = true;
    public bool SaveScreenshots { get; set; } = true;
    public bool ExtractVisibleBodyText { get; set; } = true;
    public bool IncrementalExcelSave { get; set; } = true;
    public int SaveEveryRecords { get; set; } = 1;
    public string ProfileDir { get; set; } = "profiles\\horus_fps";
    public string InputPath { get; set; } = "input\\cedulas.csv";
    public string OutputExcelPath { get; set; } = "output\\extraccion_horus_afiliados.xlsx";
    public string EvidenceDir { get; set; } = "evidence";
    public string LogDir { get; set; } = "logs";
    public string DocumentoHeader { get; set; } = "Documento";
    public List<string> TabsToExtract { get; set; } = new()
    {
        "DATOS BÁSICOS",
        "DATOS COMPLEMENTARIOS",
        "DATOS PENSIÓN",
        "DATOS TRASLADO",
        "BENEFICIARIOS",
        "HISTÓRICO NOVEDADES"
    };
}
