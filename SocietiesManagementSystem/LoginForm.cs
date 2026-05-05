using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Drawing;
using System.Data;

namespace SocietiesManagementSystem;

public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();
        UIHelper.ApplyAppTheme(this);
        this.Text = "FAST Societies Login";
        this.Size = new Size(760, 520);
        this.MinimumSize = new Size(720, 500);

        Panel header = UIHelper.CreateHeaderPanel("FAST Societies", "Sign in to manage societies, events, and members.");
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
            Width = 460,
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

        Label lblEmail = new Label { Text = "Email:" };
        UIHelper.StyleLabel(lblEmail);
        TextBox txtEmail = new TextBox { Width = 420 };
        UIHelper.StyleTextBox(txtEmail);

        Label lblPassword = new Label { Text = "Password:" };
        UIHelper.StyleLabel(lblPassword);
        TextBox txtPassword = new TextBox { Width = 420, PasswordChar = '*' };
        UIHelper.StyleTextBox(txtPassword);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 16, 0, 0)
        };
        Button btnLogin = new Button { Text = "Login", Width = 160 };
        Button btnRegister = new Button { Text = "Register", Width = 160 };
        UIHelper.StyleButton(btnLogin);
        UIHelper.StyleButton(btnRegister);
        buttonPanel.Controls.AddRange(new Control[] { btnLogin, btnRegister });

        layout.Controls.Add(lblEmail);
        layout.Controls.Add(txtEmail);
        layout.Controls.Add(lblPassword);
        layout.Controls.Add(txtPassword);
        layout.Controls.Add(buttonPanel);
        contentPanel.Controls.Add(layout);

        PositionContentPanel(contentPanel);
        this.Resize += (s, e) => PositionContentPanel(contentPanel);

        btnLogin.Click += (s, e) => Login(txtEmail.Text, txtPassword.Text);
        btnRegister.Click += (s, e) => { new RegisterForm().Show(); this.Hide(); };
    }

    private void PositionContentPanel(Panel panel)
    {
        panel.Left = Math.Max(24, (this.ClientSize.Width - panel.Width) / 2);
        panel.Top = 120;
    }

    private void Login(string email, string password)
    {
        string hashedPassword = HashPassword(password);
        string query = "SELECT UserID, Role FROM Users WHERE Email = @Email AND Password = @Password";
        SqlParameter[] parameters = {
            new SqlParameter("@Email", email),
            new SqlParameter("@Password", hashedPassword)
        };
        DataTable dt = DBHelper.ExecuteQuery(query, parameters);
        if (dt.Rows.Count > 0)
        {
            int userId = (int)dt.Rows[0]["UserID"];
            string role = dt.Rows[0]["Role"].ToString();
            Form dashboard = role switch
            {
                "Student" => new StudentDashboard(userId),
                "SocietyHead" => new SocietyDashboard(userId),
                "Admin" => new AdminDashboard(userId),
                _ => null
            };
            if (dashboard != null)
            {
                dashboard.FormClosed += (s, e) => this.Show();
                dashboard.Show();
                this.Hide();
            }
        }
        else
        {
            MessageBox.Show("Invalid credentials");
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
