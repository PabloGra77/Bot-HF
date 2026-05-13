using System.Drawing;
using System.Windows.Forms;
using HorusAfiliadosExtractor.App.Models;

namespace HorusAfiliadosExtractor.App.Forms;

public sealed class LoginForm : Form
{
    private readonly ComboBox _cboType = new();
    private readonly TextBox _txtEmail = new();
    private readonly TextBox _txtPassword = new();
    private readonly TextBox _txtInput = new();
    private readonly Button _btnInputBrowse = new();
    private readonly Button _btnOk = new();
    private readonly Button _btnCancel = new();
    private readonly CheckBox _chkShowPassword = new();

    public LoginCredentials Credentials { get; private set; } = new();
    public string InputPath { get; private set; } = string.Empty;

    public LoginForm(string defaultInputPath)
    {
        InputPath = defaultInputPath;
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Bot HF - Extractor de Afiliados Horus FPS";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 340);
        Font = new Font("Segoe UI", 9F);

        var title = new Label
        {
            Text = "Inicio de sesión Horus FPS",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(21, 101, 192),
            AutoSize = true,
            Location = new Point(20, 18)
        };

        var lblType = new Label { Text = "Tipo de usuario", Location = new Point(20, 70), AutoSize = true };
        _cboType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboType.Items.AddRange(new object[] { "Afiliado", "Funcionario" });
        _cboType.SelectedIndex = 0;
        _cboType.Location = new Point(160, 67);
        _cboType.Size = new Size(340, 25);

        var lblEmail = new Label { Text = "Correo electrónico", Location = new Point(20, 110), AutoSize = true };
        _txtEmail.Location = new Point(160, 107);
        _txtEmail.Size = new Size(340, 25);

        var lblPwd = new Label { Text = "Contraseña", Location = new Point(20, 150), AutoSize = true };
        _txtPassword.UseSystemPasswordChar = true;
        _txtPassword.Location = new Point(160, 147);
        _txtPassword.Size = new Size(340, 25);

        _chkShowPassword.Text = "Mostrar contraseña";
        _chkShowPassword.Location = new Point(160, 175);
        _chkShowPassword.AutoSize = true;
        _chkShowPassword.CheckedChanged += (_, _) => _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;

        var lblInput = new Label { Text = "Archivo de cédulas", Location = new Point(20, 210), AutoSize = true };
        _txtInput.Location = new Point(160, 207);
        _txtInput.Size = new Size(260, 25);
        _txtInput.Text = InputPath;
        _btnInputBrowse.Text = "Examinar...";
        _btnInputBrowse.Location = new Point(425, 206);
        _btnInputBrowse.Size = new Size(75, 27);
        _btnInputBrowse.Click += OnBrowseInput;

        var lblNote = new Label
        {
            Text = "Las credenciales no se guardan. Solo se usan durante esta ejecución.",
            ForeColor = Color.DimGray,
            Location = new Point(20, 248),
            AutoSize = true
        };

        _btnOk.Text = "Iniciar extracción";
        _btnOk.Location = new Point(280, 285);
        _btnOk.Size = new Size(140, 32);
        _btnOk.BackColor = Color.FromArgb(21, 101, 192);
        _btnOk.ForeColor = Color.White;
        _btnOk.FlatStyle = FlatStyle.Flat;
        _btnOk.Click += OnOk;

        _btnCancel.Text = "Cancelar";
        _btnCancel.Location = new Point(425, 285);
        _btnCancel.Size = new Size(75, 32);
        _btnCancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[] {
            title, lblType, _cboType, lblEmail, _txtEmail, lblPwd, _txtPassword,
            _chkShowPassword, lblInput, _txtInput, _btnInputBrowse, lblNote,
            _btnOk, _btnCancel
        });

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private void OnBrowseInput(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Seleccionar archivo de cédulas",
            Filter = "CSV o Excel|*.csv;*.xlsx;*.xlsm|CSV|*.csv|Excel|*.xlsx;*.xlsm|Todos|*.*",
            CheckFileExists = true,
            InitialDirectory = Path.GetDirectoryName(_txtInput.Text) ?? Environment.CurrentDirectory
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtInput.Text = dlg.FileName;
    }

    private void OnOk(object? sender, EventArgs e)
    {
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
        if (string.IsNullOrWhiteSpace(_txtInput.Text) || !File.Exists(_txtInput.Text))
        {
            MessageBox.Show(this, "El archivo de cédulas no existe.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtInput.Focus();
            return;
        }

        Credentials = new LoginCredentials
        {
            Type = _cboType.SelectedIndex == 1 ? UserType.Funcionario : UserType.Afiliado,
            Email = _txtEmail.Text.Trim(),
            Password = _txtPassword.Text
        };
        InputPath = _txtInput.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}
