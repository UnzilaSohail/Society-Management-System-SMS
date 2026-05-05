using System.Drawing;
using System.Windows.Forms;

namespace SocietiesManagementSystem;

public partial class CreateUserForm : Form
{
    public string FirstName => txtFirstName.Text.Trim();
    public string LastName => txtLastName.Text.Trim();
    public string Email => txtEmail.Text.Trim();
    public string Password => txtPassword.Text;
    public string Role => cmbRole.SelectedItem?.ToString() ?? "Student";

    private TextBox txtFirstName;
    private TextBox txtLastName;
    private TextBox txtEmail;
    private TextBox txtPassword;
    private ComboBox cmbRole;
    private Button btnSave;

    public CreateUserForm()
    {
        UIHelper.ApplyAppTheme(this);
        this.Text = "Create User";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Size = new Size(500, 620);
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // ── Main layout panel (fills entire form) ──────────────
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // header row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // form row
        this.Controls.Add(mainLayout);

        // ── Header ─────────────────────────────────────────────
        Panel header = UIHelper.CreateHeaderPanel("Create User", "Fill in the details to create a new account.");
        header.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(header, 0, 0);

        // ── Scrollable form panel ──────────────────────────────
        Panel formPanel = new Panel
        {
            BackColor = UIHelper.PanelColor,
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0)
        };
        mainLayout.Controls.Add(formPanel, 0, 1);

        // ── Field builder helpers ──────────────────────────────
        int x = 30;
        int y = 20;
        const int labelH  = 22;
        const int inputH  = 38;
        const int spacing = 16;

        Label MakeLabel(string text)
        {
            var lbl = new Label { Text = text, AutoSize = true, Location = new Point(x, y) };
            UIHelper.StyleLabel(lbl);
            return lbl;
        }

        TextBox MakeTextBox(bool password = false)
        {
            var txt = new TextBox
            {
                Width = 400,
                Height = inputH,
                Location = new Point(x, y),
                PasswordChar = password ? '*' : '\0'
            };
            UIHelper.StyleTextBox(txt);
            return txt;
        }

        // ── First Name ─────────────────────────────────────────
        var lblFirst = MakeLabel("First Name *");
        formPanel.Controls.Add(lblFirst);
        y += labelH + 4;

        txtFirstName = MakeTextBox();
        formPanel.Controls.Add(txtFirstName);
        y += inputH + spacing;

        // ── Last Name ──────────────────────────────────────────
        var lblLast = MakeLabel("Last Name *");
        formPanel.Controls.Add(lblLast);
        y += labelH + 4;

        txtLastName = MakeTextBox();
        formPanel.Controls.Add(txtLastName);
        y += inputH + spacing;

        // ── Email ──────────────────────────────────────────────
        var lblEmail = MakeLabel("Email Address *");
        formPanel.Controls.Add(lblEmail);
        y += labelH + 4;

        txtEmail = MakeTextBox();
        formPanel.Controls.Add(txtEmail);
        y += inputH + spacing;

        // ── Password ───────────────────────────────────────────
        var lblPass = MakeLabel("Temporary Password *");
        formPanel.Controls.Add(lblPass);
        y += labelH + 4;

        txtPassword = MakeTextBox(password: true);
        formPanel.Controls.Add(txtPassword);
        y += inputH + spacing;

        // ── Role ───────────────────────────────────────────────
        var lblRole = MakeLabel("Role *");
        formPanel.Controls.Add(lblRole);
        y += labelH + 4;

        cmbRole = new ComboBox
        {
            Width = 400,
            Height = inputH,
            Location = new Point(x, y),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbRole.Items.AddRange(new string[] { "Student", "SocietyHead", "Admin" });
        cmbRole.SelectedIndex = 0;
        UIHelper.StyleComboBox(cmbRole);
        formPanel.Controls.Add(cmbRole);
        y += inputH + spacing + 10;

        // ── Save Button ────────────────────────────────────────
        btnSave = new Button
        {
            Text = "Create User",
            Width = 180,
            Height = 44,
            Location = new Point(x, y)
        };
        UIHelper.StyleButton(btnSave);
        formPanel.Controls.Add(btnSave);

        btnSave.Click += (s, e) => SaveAndClose();
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName)  ||
            string.IsNullOrWhiteSpace(Email)     ||
            string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show("All fields are required.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Email.Contains("@"))
        {
            MessageBox.Show("Please enter a valid email address.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}