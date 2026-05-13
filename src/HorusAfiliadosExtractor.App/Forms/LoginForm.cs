using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using HorusAfiliadosExtractor.App.Models;

namespace HorusAfiliadosExtractor.App.Forms;

public sealed class LoginForm : Form
{
    private static readonly Color BrandPrimary = Color.FromArgb(21, 101, 192);
    private static readonly Color BrandPrimaryDark = Color.FromArgb(13, 71, 161);
    private static readonly Color CardBackground = Color.White;
    private static readonly Color FormBackground = Color.FromArgb(244, 247, 251);
    private static readonly Color TextPrimary = Color.FromArgb(33, 37, 41);
    private static readonly Color TextSecondary = Color.FromArgb(108, 117, 125);
    private static readonly Color CardBorder = Color.FromArgb(225, 230, 237);

    private readonly RadioButton _rbAfiliado = new();
    private readonly RadioButton _rbFuncionario = new();
    private readonly TextBox _txtEmail = new();
    private readonly TextBox _txtPassword = new();
    private readonly CheckBox _chkShowPassword = new();
    private readonly TextBox _txtFilePath = new();
    private readonly Button _btnAttach = new();
    private readonly Button _btnDownloadTemplate = new();
    private readonly Button _btnStart = new();
    private readonly Button _btnCancel = new();
    private readonly Label _lblFileBadge = new();
    private readonly Label _lblUpdate = new();

    public LoginCredentials Credentials { get; private set; } = new();
    public string InputPath { get; private set; } = string.Empty;

    public LoginForm(string defaultInputPath)
    {
        InputPath = defaultInputPath;
        BuildUi();
        WireUpdates();
        RefreshFileBadge();
    }

    private void BuildUi()
    {
        Text = "Bot HF - Extractor de Afiliados Horus FPS";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(640, 660);
        Font = new Font("Segoe UI", 9.5F);
        BackColor = FormBackground;
        DoubleBuffered = true;

        // ===== Header =====
        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(ClientSize.Width, 84),
            BackColor = BrandPrimary
        };
        header.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(header.ClientRectangle, BrandPrimary, BrandPrimaryDark, LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
        };

        var lblTitle = new Label
        {
            Text = "Bot HF",
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(24, 14),
            BackColor = Color.Transparent
        };
        var lblSubtitle = new Label
        {
            Text = "Extractor de Afiliados — Horus FPS",
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(220, 230, 245),
            AutoSize = true,
            Location = new Point(26, 52),
            BackColor = Color.Transparent
        };
        header.Controls.Add(lblTitle);
        header.Controls.Add(lblSubtitle);

        // ===== Card 1: Archivo de cédulas =====
        var card1 = MakeCard(new Point(20, 100), new Size(600, 150), "1. Archivo de cédulas",
            "Adjunte el archivo CSV o Excel con la columna 'Documento'.");

        _txtFilePath.Location = new Point(24, 80);
        _txtFilePath.Size = new Size(380, 26);
        _txtFilePath.Font = new Font("Segoe UI", 9.5F);
        _txtFilePath.Text = InputPath;
        _txtFilePath.TextChanged += (_, _) => RefreshFileBadge();

        StylePrimary(_btnAttach, "Adjuntar archivo");
        _btnAttach.Location = new Point(410, 78);
        _btnAttach.Size = new Size(170, 30);
        _btnAttach.Click += OnBrowseInput;

        StyleSecondary(_btnDownloadTemplate, "Descargar plantilla de ejemplo");
        _btnDownloadTemplate.Location = new Point(24, 114);
        _btnDownloadTemplate.Size = new Size(245, 28);
        _btnDownloadTemplate.Click += OnDownloadTemplate;

        _lblFileBadge.Location = new Point(280, 117);
        _lblFileBadge.AutoSize = false;
        _lblFileBadge.Size = new Size(300, 22);
        _lblFileBadge.Font = new Font("Segoe UI", 9F, FontStyle.Italic);

        card1.Controls.Add(_txtFilePath);
        card1.Controls.Add(_btnAttach);
        card1.Controls.Add(_btnDownloadTemplate);
        card1.Controls.Add(_lblFileBadge);

        // ===== Card 2: Tipo de usuario =====
        var card2 = MakeCard(new Point(20, 260), new Size(600, 92), "2. Tipo de usuario", null);

        _rbAfiliado.Text = "Afiliado";
        _rbAfiliado.Font = new Font("Segoe UI", 10F);
        _rbAfiliado.Location = new Point(24, 56);
        _rbAfiliado.AutoSize = true;
        _rbAfiliado.Checked = true;

        _rbFuncionario.Text = "Funcionario";
        _rbFuncionario.Font = new Font("Segoe UI", 10F);
        _rbFuncionario.Location = new Point(160, 56);
        _rbFuncionario.AutoSize = true;

        card2.Controls.Add(_rbAfiliado);
        card2.Controls.Add(_rbFuncionario);

        // ===== Card 3: Credenciales =====
        var card3 = MakeCard(new Point(20, 362), new Size(600, 175), "3. Credenciales de Horus FPS",
            "No se guardan. Solo se usan durante esta ejecución.");

        var lblEmail = new Label
        {
            Text = "Correo electrónico",
            Location = new Point(24, 80),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9F)
        };
        _txtEmail.Location = new Point(24, 100);
        _txtEmail.Size = new Size(556, 26);
        _txtEmail.Font = new Font("Segoe UI", 10F);

        var lblPwd = new Label
        {
            Text = "Contraseña",
            Location = new Point(24, 132),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9F)
        };
        _txtPassword.Location = new Point(24, 152);
        _txtPassword.Size = new Size(420, 26);
        _txtPassword.UseSystemPasswordChar = true;
        _txtPassword.Font = new Font("Segoe UI", 10F);

        _chkShowPassword.Text = "Mostrar";
        _chkShowPassword.Location = new Point(460, 155);
        _chkShowPassword.AutoSize = true;
        _chkShowPassword.Font = new Font("Segoe UI", 9F);
        _chkShowPassword.CheckedChanged += (_, _) => _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;

        card3.Controls.Add(lblEmail);
        card3.Controls.Add(_txtEmail);
        card3.Controls.Add(lblPwd);
        card3.Controls.Add(_txtPassword);
        card3.Controls.Add(_chkShowPassword);

        // ===== Footer =====
        StylePrimary(_btnStart, "Iniciar extracción");
        _btnStart.Location = new Point(380, 555);
        _btnStart.Size = new Size(180, 38);
        _btnStart.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        _btnStart.Click += OnOk;

        StyleSecondary(_btnCancel, "Cancelar");
        _btnCancel.Location = new Point(280, 559);
        _btnCancel.Size = new Size(90, 30);
        _btnCancel.DialogResult = DialogResult.Cancel;

        _lblUpdate.Location = new Point(20, 615);
        _lblUpdate.AutoSize = false;
        _lblUpdate.Size = new Size(600, 18);
        _lblUpdate.ForeColor = TextSecondary;
        _lblUpdate.Font = new Font("Segoe UI", 8.5F);

        var lblVersion = new Label
        {
            Text = $"v{Application.ProductVersion}",
            Location = new Point(20, 633),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8F)
        };

        Controls.Add(header);
        Controls.Add(card1);
        Controls.Add(card2);
        Controls.Add(card3);
        Controls.Add(_btnStart);
        Controls.Add(_btnCancel);
        Controls.Add(_lblUpdate);
        Controls.Add(lblVersion);

        AcceptButton = _btnStart;
        CancelButton = _btnCancel;
    }

    private Panel MakeCard(Point location, Size size, string title, string? subtitle)
    {
        var card = new Panel
        {
            Location = location,
            Size = size,
            BackColor = CardBackground
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(CardBorder);
            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            Location = new Point(20, 16),
            BackColor = Color.Transparent
        };
        card.Controls.Add(lblTitle);

        if (!string.IsNullOrEmpty(subtitle))
        {
            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(20, 40),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblSub);
        }

        return card;
    }

    private static void StylePrimary(Button btn, string text)
    {
        btn.Text = text;
        btn.BackColor = BrandPrimary;
        btn.ForeColor = Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (_, _) => btn.BackColor = BrandPrimaryDark;
        btn.MouseLeave += (_, _) => btn.BackColor = BrandPrimary;
        btn.Font = new Font("Segoe UI Semibold", 9.5F);
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

    private void WireUpdates()
    {
        Program.Updater.StatusChanged += msg =>
        {
            if (IsDisposed) return;
            try { BeginInvoke(() => _lblUpdate.Text = msg); } catch { }
        };
        Program.Updater.UpdateReady += version =>
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke(() =>
                {
                    _lblUpdate.Text = $"Actualización {version} lista. Se aplicará al cerrar.";
                    _lblUpdate.ForeColor = Color.SeaGreen;
                });
            }
            catch { }
        };
    }

    private void RefreshFileBadge()
    {
        var path = _txtFilePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _lblFileBadge.Text = "Sin archivo seleccionado";
            _lblFileBadge.ForeColor = Color.IndianRed;
            return;
        }
        try
        {
            var info = new FileInfo(path);
            _lblFileBadge.Text = $"✓ {info.Name}  ({info.Length / 1024.0:0.#} KB)";
            _lblFileBadge.ForeColor = Color.SeaGreen;
        }
        catch
        {
            _lblFileBadge.Text = "Archivo no accesible";
            _lblFileBadge.ForeColor = Color.IndianRed;
        }
    }

    private void OnBrowseInput(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Seleccionar archivo de cédulas",
            Filter = "CSV o Excel|*.csv;*.xlsx;*.xlsm|CSV|*.csv|Excel|*.xlsx;*.xlsm|Todos|*.*",
            CheckFileExists = true,
            InitialDirectory = SafeDir(_txtFilePath.Text)
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtFilePath.Text = dlg.FileName;
            RefreshFileBadge();
        }
    }

    private void OnDownloadTemplate(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Guardar plantilla de cédulas",
            Filter = "CSV|*.csv",
            FileName = "cedulas_plantilla.csv",
            InitialDirectory = SafeDir(_txtFilePath.Text)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var content = ReadEmbeddedTemplate();
            File.WriteAllText(dlg.FileName, content, new System.Text.UTF8Encoding(true));
            var result = MessageBox.Show(this,
                $"Plantilla guardada en:\n{dlg.FileName}\n\n¿Desea abrirla ahora?",
                "Plantilla generada", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"No se pudo guardar la plantilla.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string ReadEmbeddedTemplate()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("cedulas.template.csv", StringComparison.OrdinalIgnoreCase));
        if (name == null) return "Documento\n";
        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string SafeDir(string path)
    {
        try
        {
            var d = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(d) && Directory.Exists(d)) return d;
        }
        catch { }
        return Environment.CurrentDirectory;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        var path = _txtFilePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            MessageBox.Show(this, "Seleccione un archivo de cédulas válido (CSV o Excel).", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _btnAttach.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_txtEmail.Text))
        {
            MessageBox.Show(this, "Ingrese el correo electrónico.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtEmail.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_txtPassword.Text))
        {
            MessageBox.Show(this, "Ingrese la contraseña.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPassword.Focus();
            return;
        }

        Credentials = new LoginCredentials
        {
            Type = _rbFuncionario.Checked ? UserType.Funcionario : UserType.Afiliado,
            Email = _txtEmail.Text.Trim(),
            Password = _txtPassword.Text
        };
        InputPath = path;
        DialogResult = DialogResult.OK;
        Close();
    }
}
