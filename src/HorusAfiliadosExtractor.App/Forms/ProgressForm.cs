using System.Drawing;
using System.Windows.Forms;
using HorusAfiliadosExtractor.App.Services;

namespace HorusAfiliadosExtractor.App.Forms;

public sealed class ProgressForm : Form
{
    private static readonly Color BrandPrimary = Color.FromArgb(21, 101, 192);
    private static readonly Color ColorPause = Color.FromArgb(255, 152, 0);
    private static readonly Color ColorResume = Color.FromArgb(76, 175, 80);
    private static readonly Color ColorStop = Color.FromArgb(211, 47, 47);

    private readonly TextBox _txtLog = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _lblStatus = new();
    private readonly Button _btnPause = new();
    private readonly Button _btnResume = new();
    private readonly Button _btnFinalize = new();
    private readonly Button _btnOpenOutput = new();
    private readonly Button _btnOpenFolder = new();
    private readonly Button _btnClose = new();
    private string _outputPath = string.Empty;

    public event Action? PauseRequested;
    public event Action? ResumeRequested;
    public event Action? FinalizeRequested;

    public ProgressForm()
    {
        BuildUi();
        SetRunningState();
    }

    private void BuildUi()
    {
        Text = "Bot HF - Extracción en curso";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 540);
        MinimumSize = new Size(680, 420);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(244, 247, 251);

        _lblStatus.Text = "Iniciando...";
        _lblStatus.Location = new Point(15, 12);
        _lblStatus.AutoSize = true;
        _lblStatus.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

        _progress.Location = new Point(15, 38);
        _progress.Size = new Size(790, 18);
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // ---- Toolbar con botones de control ----
        StyleControlButton(_btnPause, "⏸  Detener", ColorPause);
        _btnPause.Location = new Point(15, 70);
        _btnPause.Size = new Size(130, 34);
        _btnPause.Click += (_, _) => PauseRequested?.Invoke();

        StyleControlButton(_btnResume, "▶  Continuar", ColorResume);
        _btnResume.Location = new Point(155, 70);
        _btnResume.Size = new Size(130, 34);
        _btnResume.Click += (_, _) => ResumeRequested?.Invoke();

        StyleControlButton(_btnFinalize, "⏹  Finalizar", ColorStop);
        _btnFinalize.Location = new Point(295, 70);
        _btnFinalize.Size = new Size(130, 34);
        _btnFinalize.Click += OnFinalizeClick;

        // ---- Log ----
        _txtLog.Location = new Point(15, 115);
        _txtLog.Size = new Size(790, 380);
        _txtLog.Multiline = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.ReadOnly = true;
        _txtLog.BackColor = Color.FromArgb(30, 30, 30);
        _txtLog.ForeColor = Color.Gainsboro;
        _txtLog.Font = new Font("Consolas", 9F);
        _txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        // ---- Bottom action buttons ----
        StyleSecondary(_btnOpenOutput, "Abrir Excel resultado");
        _btnOpenOutput.Location = new Point(15, 505);
        _btnOpenOutput.Size = new Size(170, 30);
        _btnOpenOutput.Enabled = false;
        _btnOpenOutput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnOpenOutput.Click += (_, _) =>
        {
            if (File.Exists(_outputPath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_outputPath) { UseShellExecute = true });
        };

        StyleSecondary(_btnOpenFolder, "Abrir carpeta de resultados");
        _btnOpenFolder.Location = new Point(195, 505);
        _btnOpenFolder.Size = new Size(200, 30);
        _btnOpenFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnOpenFolder.Click += (_, _) =>
        {
            var dir = string.IsNullOrWhiteSpace(_outputPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "BotHF")
                : Path.GetDirectoryName(Path.GetDirectoryName(_outputPath)) ?? string.Empty;
            if (Directory.Exists(dir))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        };

        StyleSecondary(_btnClose, "Cerrar");
        _btnClose.Location = new Point(730, 505);
        _btnClose.Size = new Size(75, 30);
        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            _lblStatus, _progress,
            _btnPause, _btnResume, _btnFinalize,
            _txtLog,
            _btnOpenOutput, _btnOpenFolder, _btnClose
        });
    }

    private static void StyleControlButton(Button btn, string text, Color color)
    {
        btn.Text = text;
        btn.BackColor = color;
        btn.ForeColor = Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.Font = new Font("Segoe UI Semibold", 10F);
        btn.TextAlign = ContentAlignment.MiddleCenter;
    }

    private static void StyleSecondary(Button btn, string text)
    {
        btn.Text = text;
        btn.BackColor = Color.White;
        btn.ForeColor = BrandPrimary;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = BrandPrimary;
        btn.FlatAppearance.BorderSize = 1;
        btn.Cursor = Cursors.Hand;
        btn.Font = new Font("Segoe UI", 9F);
    }

    private void OnFinalizeClick(object? sender, EventArgs e)
    {
        var ans = MessageBox.Show(this,
            "¿Detener la extracción ahora?\n\nSe guardará el Excel con lo procesado hasta este momento.",
            "Finalizar extracción",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (ans == DialogResult.Yes)
        {
            FinalizeRequested?.Invoke();
            SetFinalizingState();
        }
    }

    // ---- State control ----
    public void SetRunningState()
    {
        if (InvokeRequired) { Invoke(SetRunningState); return; }
        _btnPause.Enabled = true;
        _btnResume.Enabled = false;
        _btnFinalize.Enabled = true;
        _btnClose.Enabled = false;
    }

    public void SetPausedState()
    {
        if (InvokeRequired) { Invoke(SetPausedState); return; }
        _btnPause.Enabled = false;
        _btnResume.Enabled = true;
        _btnFinalize.Enabled = true;
        _lblStatus.ForeColor = ColorPause;
    }

    public void SetResumedState()
    {
        if (InvokeRequired) { Invoke(SetResumedState); return; }
        _btnPause.Enabled = true;
        _btnResume.Enabled = false;
        _btnFinalize.Enabled = true;
        _lblStatus.ForeColor = Color.Black;
    }

    public void SetFinalizingState()
    {
        if (InvokeRequired) { Invoke(SetFinalizingState); return; }
        _btnPause.Enabled = false;
        _btnResume.Enabled = false;
        _btnFinalize.Enabled = false;
        _lblStatus.Text = "Finalizando — guardando lo procesado...";
        _lblStatus.ForeColor = ColorStop;
    }

    public void SetTotal(int total)
    {
        if (InvokeRequired) { Invoke(() => SetTotal(total)); return; }
        _progress.Maximum = Math.Max(1, total);
        _progress.Value = 0;
    }

    public void SetProgress(int current, int total, string status)
    {
        if (InvokeRequired) { Invoke(() => SetProgress(current, total, status)); return; }
        _progress.Maximum = Math.Max(1, total);
        _progress.Value = Math.Min(current, _progress.Maximum);
        _lblStatus.Text = $"[{current}/{total}] {status}";
    }

    public void AppendLog(string line)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(line)); return; }
        _txtLog.AppendText(line + Environment.NewLine);
    }

    public void Finish(string outputExcelPath, bool success)
    {
        if (InvokeRequired) { Invoke(() => Finish(outputExcelPath, success)); return; }
        _outputPath = outputExcelPath;
        _btnPause.Enabled = false;
        _btnResume.Enabled = false;
        _btnFinalize.Enabled = false;
        _btnClose.Enabled = true;
        _btnOpenOutput.Enabled = File.Exists(outputExcelPath);
        _lblStatus.Text = success
            ? "Proceso finalizado correctamente."
            : "Proceso finalizado con errores o cancelado. Revise el log.";
        _lblStatus.ForeColor = success ? Color.SeaGreen : ColorStop;
    }
}
