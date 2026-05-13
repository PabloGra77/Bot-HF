using System.Drawing;
using System.Windows.Forms;

namespace HorusAfiliadosExtractor.App.Forms;

public sealed class ProgressForm : Form
{
    private readonly TextBox _txtLog = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _lblStatus = new();
    private readonly Button _btnClose = new();
    private readonly Button _btnOpenOutput = new();
    private string _outputPath = string.Empty;

    public ProgressForm()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Bot HF - Extracción en curso";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 480);
        MinimumSize = new Size(620, 380);
        Font = new Font("Segoe UI", 9F);

        _lblStatus.Text = "Iniciando...";
        _lblStatus.Location = new Point(15, 12);
        _lblStatus.AutoSize = true;
        _lblStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

        _progress.Location = new Point(15, 38);
        _progress.Size = new Size(730, 18);
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _txtLog.Location = new Point(15, 68);
        _txtLog.Size = new Size(730, 360);
        _txtLog.Multiline = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.ReadOnly = true;
        _txtLog.BackColor = Color.FromArgb(30, 30, 30);
        _txtLog.ForeColor = Color.Gainsboro;
        _txtLog.Font = new Font("Consolas", 9F);
        _txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        _btnOpenOutput.Text = "Abrir Excel resultado";
        _btnOpenOutput.Location = new Point(15, 440);
        _btnOpenOutput.Size = new Size(160, 30);
        _btnOpenOutput.Enabled = false;
        _btnOpenOutput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnOpenOutput.Click += (_, _) =>
        {
            if (File.Exists(_outputPath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_outputPath) { UseShellExecute = true });
        };

        _btnClose.Text = "Cerrar";
        _btnClose.Location = new Point(670, 440);
        _btnClose.Size = new Size(75, 30);
        _btnClose.Enabled = false;
        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { _lblStatus, _progress, _txtLog, _btnOpenOutput, _btnClose });
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
        _btnClose.Enabled = true;
        _btnOpenOutput.Enabled = File.Exists(outputExcelPath);
        _lblStatus.Text = success ? "Proceso finalizado correctamente." : "Proceso finalizado con errores. Revise el log.";
        _lblStatus.ForeColor = success ? Color.SeaGreen : Color.IndianRed;
    }
}
