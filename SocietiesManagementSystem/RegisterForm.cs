using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Drawing;
using System.Data;

namespace SocietiesManagementSystem;

public partial class RegisterForm : Form
{
    public RegisterForm()
    {
        InitializeComponent();
        UIHelper.ApplyAppTheme(this);
        this.Text = "FAST Societies Registration";
        this.Size = new Size(820, 640);
        this.MinimumSize = new Size(780, 620);

        Panel header = UIHelper.CreateHeaderPanel("Register Account", "Create a new student or society head account.");
        this.Controls.Add(header);

        Panel formPanel = new Panel
        {
            BackColor = UIHelper.PanelColor,
            Dock = DockStyle.Fill,
            Padding = new Padding(24)
        };
        this.Controls.Add(formPanel);

        Panel contentPanel = new Panel
        {
            Width = 520,
            AutoSize = true,
            BackColor = Color.FromArgb(34, 42, 58)
        };
        formPanel.Controls.Add(contentPanel);

        TableLayoutPanel layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(14)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label lblFirstName = new Label { Text = "First Name:" };
        UIHelper.StyleLabel(lblFirstName);
        TextBox txtFirstName = new TextBox { Width = 480 };
        UIHelper.StyleTextBox(txtFirstName);

        Label lblLastName = new Label { Text = "Last Name:" };
        UIHelper.StyleLabel(lblLastName);
        TextBox txtLastName = new TextBox { Width = 480 };
        UIHelper.StyleTextBox(txtLastName);

        Label lblEmail = new Label { Text = "Email:" };
        UIHelper.StyleLabel(lblEmail);
        TextBox txtEmail = new TextBox { Width = 480 };
        UIHelper.StyleTextBox(txtEmail);

        Label lblPassword = new Label { Text = "Password:" };
        UIHelper.StyleLabel(lblPassword);
        TextBox txtPassword = new TextBox { Width = 480, PasswordChar = '*' };
        UIHelper.StyleTextBox(txtPassword);

        Label lblRole = new Label { Text = "Role:" };
        UIHelper.StyleLabel(lblRole);
        ComboBox cmbRole = new ComboBox { Width = 480, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbRole.Items.AddRange(new string[] { "Student", "SocietyHead" });
        cmbRole.SelectedIndex = 0;
        UIHelper.StyleComboBox(cmbRole);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 16, 0, 0)
        };
        Button btnRegister = new Button { Text = "Register", Width = 180 };
        Button btnBack = new Button { Text = "Back to Login", Width = 180 };
        UIHelper.StyleButton(btnRegister);
        UIHelper.StyleButton(btnBack);
        buttonPanel.Controls.AddRange(new Control[] { btnRegister, btnBack });

        layout.Controls.Add(lblFirstName);
        layout.Controls.Add(txtFirstName);
        layout.Controls.Add(lblLastName);
        layout.Controls.Add(txtLastName);
        layout.Controls.Add(lblEmail);
        layout.Controls.Add(txtEmail);
        layout.Controls.Add(lblPassword);
        layout.Controls.Add(txtPassword);
        layout.Controls.Add(lblRole);
        layout.Controls.Add(cmbRole);
        layout.Controls.Add(buttonPanel);
        contentPanel.Controls.Add(layout);

        PositionContentPanel(contentPanel);
        this.Resize += (s, e) => PositionContentPanel(contentPanel);

        btnRegister.Click += (s, e) => Register(txtFirstName.Text, txtLastName.Text, txtEmail.Text, txtPassword.Text, cmbRole.SelectedItem?.ToString() ?? "Student");
        btnBack.Click += (s, e) => { new LoginForm().Show(); this.Close(); };
    }

    private void PositionContentPanel(Panel panel)
    {
        panel.Left = Math.Max(24, (this.ClientSize.Width - panel.Width) / 2);
        panel.Top = 120;
    }

    private void Register(string firstName, string lastName, string email, string password, string role)
    {
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("All fields are required");
            return;
        }
        string hashedPassword = HashPassword(password);
        string query = "INSERT INTO Users (FirstName, LastName, Email, Password, Role) VALUES (@FirstName, @LastName, @Email, @Password, @Role)";
        SqlParameter[] parameters = {
            new SqlParameter("@FirstName", firstName),
            new SqlParameter("@LastName", lastName),
            new SqlParameter("@Email", email),
            new SqlParameter("@Password", hashedPassword),
            new SqlParameter("@Role", role)
        };
        try
        {
            DBHelper.ExecuteNonQuery(query, parameters);
            MessageBox.Show("Registration successful");
            new LoginForm().Show();
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message);
        }
    }

    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}