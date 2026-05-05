using System.Data;
using Microsoft.Data.SqlClient;

namespace SocietiesManagementSystem;

public partial class AdminDashboard : Form
{
    private int userId;
    private Panel mainPanel;
    private Panel sidebar;
    private Button[] navButtons;
    private Label statusBarLabel;

    // Design tokens
    private static readonly Color BgDark       = Color.FromArgb(13,  17,  23);
    private static readonly Color BgCard       = Color.FromArgb(22,  27,  34);
    private static readonly Color BgHover      = Color.FromArgb(30,  37,  48);
    private static readonly Color AccentBlue   = Color.FromArgb(56, 139, 253);
    private static readonly Color AccentGreen  = Color.FromArgb(63, 185, 80);
    private static readonly Color AccentRed    = Color.FromArgb(248, 81,  73);
    private static readonly Color AccentOrange = Color.FromArgb(210, 153, 34);
    private static readonly Color AccentPurple = Color.FromArgb(139, 92, 246);
    private static readonly Color TextPrimary  = Color.FromArgb(230, 237, 243);
    private static readonly Color TextMuted    = Color.FromArgb(125, 133, 144);
    private static readonly Color Border       = Color.FromArgb(48,  54,  61);

    private static readonly Font FontTitle  = new Font("Segoe UI", 22f, FontStyle.Bold);
    private static readonly Font FontH2     = new Font("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font FontH3     = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
    private static readonly Font FontBody   = new Font("Segoe UI", 9.5f);
    private static readonly Font FontSmall  = new Font("Segoe UI", 8.5f);
    private static readonly Font FontMono   = new Font("Consolas", 9f);

    public AdminDashboard(int userId)
    {
        this.userId = userId;
        InitializeComponent();
        BuildUI();
    }

    // ─────────────────────────────────────────────
    //  UI Construction
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        this.Text            = "Admin Console — Societies Management System";
        this.Size            = new Size(1280, 840);
        this.MinimumSize     = new Size(1100, 700);
        this.BackColor       = BgDark;
        this.ForeColor       = TextPrimary;
        this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.Sizable;

        // ── Sidebar ──────────────────────────────
        sidebar = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 220,
            BackColor = BgCard,
        };
        this.Controls.Add(sidebar);

        // Brand
        Panel brand = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(17, 22, 30) };
        Label ico  = new Label { Text = "⬡", Font = new Font("Segoe UI", 18f), ForeColor = AccentBlue, AutoSize = true, Location = new Point(18, 20) };
        Label name = new Label { Text = "Admin", Font = new Font("Segoe UI Semibold", 13f), ForeColor = TextPrimary, AutoSize = true, Location = new Point(46, 14) };
        Label sub  = new Label { Text = "Console", Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(46, 36) };
        brand.Controls.AddRange(new Control[] { ico, name, sub });
        sidebar.Controls.Add(brand);

        // Divider
        Panel divTop = MakeDivider(DockStyle.Top);
        sidebar.Controls.Add(divTop);

        // Nav
        string[] navLabels = { "👤  Users", "🏛️  Societies", "📅  Events", "📊  Reports" };
        navButtons = new Button[navLabels.Length];
        Action<Panel>[] panels = { ShowUsersPanel, ShowSocietiesPanel, ShowEventsPanel, ShowReportsPanel };

        Panel navContainer = new Panel { Dock = DockStyle.Top, Height = navLabels.Length * 52 + 16, BackColor = BgCard };
        sidebar.Controls.Add(navContainer);

        for (int i = 0; i < navLabels.Length; i++)
        {
            int idx = i;
            Button btn = new Button
            {
                Text      = navLabels[i],
                Bounds    = new Rectangle(12, 8 + i * 52, 196, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = i == 0 ? Color.FromArgb(56, 139, 253, 30) : Color.Transparent,
                ForeColor = i == 0 ? AccentBlue : TextMuted,
                Font      = FontH3,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize       = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 37, 48);
            btn.MouseEnter += (s, e) => { if (btn.BackColor != Color.FromArgb(56, 139, 253, 30)) btn.ForeColor = TextPrimary; };
            btn.MouseLeave += (s, e) => { if (btn.ForeColor != AccentBlue) btn.ForeColor = TextMuted; };
            btn.Click += (s, e) =>
            {
                SetActiveNav(idx);
                mainPanel.Controls.Clear();
                panels[idx](mainPanel);
            };
            navButtons[i] = btn;
            navContainer.Controls.Add(btn);
        }

        // Logout at bottom
        Panel sideBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = BgCard };
        Button btnLogout = new Button
        {
            Text      = "⟵  Logout",
            Bounds    = new Rectangle(12, 10, 196, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(248, 81, 73, 25),
            ForeColor = AccentRed,
            Font      = FontH3,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            Cursor    = Cursors.Hand,
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 81, 73, 45);
        btnLogout.Click += (s, e) => this.Close();
        sideBottom.Controls.Add(btnLogout);
        sidebar.Controls.Add(sideBottom);
        sidebar.Controls.Add(MakeDivider(DockStyle.Bottom));

        // ── Right side ───────────────────────────
        Panel rightCol = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };
        this.Controls.Add(rightCol);
        this.Controls.SetChildIndex(rightCol, 0);

        // Status bar
        Panel statusBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(17, 22, 30) };
        statusBarLabel = new Label
        {
            Text      = $"Logged in as Admin  •  {DateTime.Now:ddd, dd MMM yyyy  HH:mm}",
            Font      = FontSmall,
            ForeColor = TextMuted,
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(14, 0, 0, 0),
        };
        statusBar.Controls.Add(statusBarLabel);
        rightCol.Controls.Add(statusBar);

        // Main scrollable content
        mainPanel = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = BgDark,
            AutoScroll  = true,
            Padding     = new Padding(28, 24, 28, 24),
        };
        rightCol.Controls.Add(mainPanel);

        ShowUsersPanel(mainPanel);
    }

    // ─────────────────────────────────────────────
    //  Section: Users
    // ─────────────────────────────────────────────
    private void ShowUsersPanel(Panel host)
    {
        host.Controls.Clear();

        // Header row
        Panel hdr = SectionHeader("User Management", "Create, edit and manage all student & staff accounts.", AccentBlue);
        host.Controls.Add(hdr);

        // Stats row
        int totalUsers   = SafeCount("SELECT COUNT(*) FROM Users");
        int adminCount   = SafeCount("SELECT COUNT(*) FROM Users WHERE Role='Admin'");
        int studentCount = SafeCount("SELECT COUNT(*) FROM Users WHERE Role='Student'");
        int societyCount = SafeCount("SELECT COUNT(*) FROM Users WHERE Role='SocietyHead'");

        Panel statsRow = StatRow(new (string, string, Color)[]
        {
            ("Total Users",    totalUsers.ToString(),   AccentBlue),
            ("Students",       studentCount.ToString(), AccentGreen),
            ("Society Heads",  societyCount.ToString(), AccentPurple),
            ("Admins",         adminCount.ToString(),   AccentOrange),
        }, 160);
        statsRow.Location = new Point(0, hdr.Bottom + 16);
        host.Controls.Add(statsRow);

        // Toolbar
        Panel toolbar = new Panel { BackColor = Color.Transparent, Height = 52, Width = 900 };
        toolbar.Location = new Point(0, statsRow.Bottom + 18);

        Button btnCreate = ActionButton("＋ Create User", AccentBlue);
        Button btnDelete = ActionButton("🗑  Delete User", AccentRed);
        Button btnRole   = ActionButton("⚙  Change Role", AccentOrange);
        Button btnRefresh= ActionButton("↻  Refresh", TextMuted);
        btnCreate.Location = new Point(0, 6);
        btnDelete.Location = new Point(148, 6);
        btnRole.Location   = new Point(296, 6);
        btnRefresh.Location= new Point(444, 6);
        toolbar.Controls.AddRange(new Control[] { btnCreate, btnDelete, btnRole, btnRefresh });
        host.Controls.Add(toolbar);

        // Grid
        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, toolbar.Bottom + 10);
        dgv.Size     = new Size(host.Width - 60, 420);
        host.Controls.Add(dgv);
        LoadUsers(dgv);

        btnCreate.Click  += (s, e) => CreateUser(dgv);
        btnDelete.Click  += (s, e) => DeleteUser(dgv);
        btnRole.Click    += (s, e) => ChangeUserRole(dgv);
        btnRefresh.Click += (s, e) => LoadUsers(dgv);
    }

    // ─────────────────────────────────────────────
    //  Section: Societies
    // ─────────────────────────────────────────────
    private void ShowSocietiesPanel(Panel host)
    {
        host.Controls.Clear();

        Panel hdr = SectionHeader("Society Management", "Approve, suspend, or remove societies across the university.", AccentGreen);
        host.Controls.Add(hdr);

        int total     = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status <> 'Deleted'");
        int active    = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status = 'Active'");
        int suspended = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status = 'Suspended'");

        Panel statsRow = StatRow(new (string, string, Color)[]
        {
            ("Total Societies", total.ToString(),     AccentBlue),
            ("Active",          active.ToString(),    AccentGreen),
            ("Suspended",       suspended.ToString(), AccentRed),
        }, 200);
        statsRow.Location = new Point(0, hdr.Bottom + 16);
        host.Controls.Add(statsRow);

        Panel toolbar = new Panel { BackColor = Color.Transparent, Height = 104, Width = 900 };
        toolbar.Location = new Point(0, statsRow.Bottom + 18);

        Button btnApprove = ActionButton("✔ Approve",  AccentGreen, 120);
        Button btnSuspend = ActionButton("⏸ Suspend",  AccentOrange, 120);
        Button btnDelete  = ActionButton("🗑 Delete",   AccentRed, 120);
        Button btnCreate  = ActionButton("＋ Create",   AccentBlue, 120);
        Button btnRefresh = ActionButton("↻ Refresh",  TextMuted, 120);
        btnApprove.Location = new Point(0,   6);
        btnSuspend.Location = new Point(132, 6);
        btnDelete.Location  = new Point(264, 6);
        btnCreate.Location  = new Point(396, 6);
        btnRefresh.Location = new Point(0,   50);
        toolbar.Controls.AddRange(new Control[] { btnApprove, btnSuspend, btnDelete, btnCreate, btnRefresh });
        host.Controls.Add(toolbar);

        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, toolbar.Bottom + 10);
        dgv.Size     = new Size(host.Width - 60, 420);
        host.Controls.Add(dgv);
        LoadSocieties(dgv);

        btnApprove.Click += (s, e) => ApproveSociety(dgv);
        btnSuspend.Click += (s, e) => SuspendSociety(dgv);
        btnDelete.Click  += (s, e) => DeleteSociety(dgv);
        btnCreate.Click  += (s, e) => CreateSociety(dgv);
        btnRefresh.Click += (s, e) => LoadSocieties(dgv);
    }

    // ─────────────────────────────────────────────
    //  Section: Events
    // ─────────────────────────────────────────────
    private void ShowEventsPanel(Panel host)
    {
        host.Controls.Clear();

        Panel hdr = SectionHeader("Event Management", "Review and approve event requests from societies.", AccentPurple);
        host.Controls.Add(hdr);

        int total     = SafeCount("SELECT COUNT(*) FROM Events");
        int approved  = SafeCount("SELECT COUNT(*) FROM Events WHERE Status = 'Approved'");
        int pending   = SafeCount("SELECT COUNT(*) FROM Events WHERE Status = 'Pending'");
        int cancelled = SafeCount("SELECT COUNT(*) FROM Events WHERE Status = 'Cancelled'");

        Panel statsRow = StatRow(new (string, string, Color)[]
        {
            ("Total Events",  total.ToString(),     AccentBlue),
            ("Approved",      approved.ToString(),  AccentGreen),
            ("Pending",       pending.ToString(),   AccentOrange),
            ("Cancelled",     cancelled.ToString(), AccentRed),
        }, 165);
        statsRow.Location = new Point(0, hdr.Bottom + 16);
        host.Controls.Add(statsRow);

        Panel toolbar = new Panel { BackColor = Color.Transparent, Height = 52, Width = 900 };
        toolbar.Location = new Point(0, statsRow.Bottom + 18);

        Button btnApprove = ActionButton("✔ Approve Event",  AccentGreen);
        Button btnCancel  = ActionButton("✖ Cancel Event",   AccentRed);
        Button btnRefresh = ActionButton("↻  Refresh",        TextMuted);
        btnApprove.Location = new Point(0,   6);
        btnCancel.Location  = new Point(148, 6);
        btnRefresh.Location = new Point(296, 6);
        toolbar.Controls.AddRange(new Control[] { btnApprove, btnCancel, btnRefresh });
        host.Controls.Add(toolbar);

        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, toolbar.Bottom + 10);
        dgv.Size     = new Size(host.Width - 60, 420);
        host.Controls.Add(dgv);
        LoadEvents(dgv);

        btnApprove.Click += (s, e) => ApproveEvent(dgv);
        btnCancel.Click  += (s, e) => CancelEvent(dgv);
        btnRefresh.Click += (s, e) => LoadEvents(dgv);
    }

    // ─────────────────────────────────────────────
    //  Section: Reports
    // ─────────────────────────────────────────────
    private void ShowReportsPanel(Panel host)
    {
        host.Controls.Clear();

        Panel hdr = SectionHeader("University Reports", "Generate and export system-wide analytics and summaries.", AccentOrange);
        host.Controls.Add(hdr);

        // Report card grid
        var cards = new (string title, string desc, Color accent)[]
        {
            ("Users",       "Total registered accounts by role", AccentBlue),
            ("Societies",   "Active, pending and suspended",      AccentGreen),
            ("Events",      "All events and approval rates",      AccentPurple),
            ("Memberships", "Approved vs pending requests",       AccentOrange),
        };

        int cardW = 200, cardH = 110, gap = 16, startX = 0, startY = hdr.Bottom + 20;
        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            Panel card = RoundCard(cardW, cardH, c.accent);
            card.Location = new Point(startX + i * (cardW + gap), startY);
            Label lTitle = new Label { Text = c.title, Font = FontH3, ForeColor = TextPrimary, AutoSize = true, Location = new Point(14, 14) };
            Label lDesc  = new Label { Text = c.desc,  Font = FontSmall, ForeColor = TextMuted, AutoSize = false, Size = new Size(cardW - 28, 40), Location = new Point(14, 38) };
            Panel accent = new Panel { BackColor = c.accent, Size = new Size(3, cardH - 28), Location = new Point(0, 14) };
            card.Controls.AddRange(new Control[] { accent, lTitle, lDesc });
            host.Controls.Add(card);
        }

        // Report output
        Panel reportBox = new Panel
        {
            Location  = new Point(0, startY + cardH + 24),
            Size      = new Size(host.Width - 60, 300),
            BackColor = BgCard,
            AutoScroll = true,
        };
        reportBox.Paint += (s, e) => DrawBorder(e.Graphics, reportBox.ClientRectangle, Border);
        host.Controls.Add(reportBox);

        RichTextBox reportLabel = new RichTextBox
        {
            Text              = "Click 'Generate Report' to view university-wide statistics.",
            Font              = FontMono,
            ForeColor         = TextMuted,
            BackColor         = BgCard,
            BorderStyle       = BorderStyle.None,
            ReadOnly          = true,
            Dock              = DockStyle.Fill,
            ScrollBars        = RichTextBoxScrollBars.Vertical,
            WordWrap          = true,
            Margin            = new Padding(0),
        };
        reportBox.Controls.Add(reportLabel);

        Panel btnRow = new Panel { BackColor = Color.Transparent, Height = 52, Width = 600 };
        btnRow.Location = new Point(0, reportBox.Bottom + 16);

        Button btnGenerate = ActionButton("▶ Generate Report", AccentOrange, 200);
        Button btnExport   = ActionButton("⬇ Export to TXT",   AccentBlue,   160);
        btnGenerate.Location = new Point(0,   6);
        btnExport.Location   = new Point(210, 6);
        btnRow.Controls.AddRange(new Control[] { btnGenerate, btnExport });
        host.Controls.Add(btnRow);

        string lastReport = "";
        btnGenerate.Click += (s, e) =>
        {
            lastReport = GenerateAdminReport();
            reportLabel.Text = lastReport;
            reportLabel.ForeColor = TextPrimary;
            reportLabel.SelectAll();
            reportLabel.SelectionColor = TextPrimary;
        };
        btnExport.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(lastReport)) { ShowToast("Generate a report first."); return; }
            using SaveFileDialog sfd = new SaveFileDialog { Filter = "Text File|*.txt", FileName = $"UniReport_{DateTime.Now:yyyyMMdd_HHmm}.txt" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, lastReport);
                ShowToast("Report exported successfully.");
            }
        };
    }

    // ─────────────────────────────────────────────
    //  Data Methods
    // ─────────────────────────────────────────────
    private void LoadUsers(DataGridView dgv)
    {
        try
        {
            string query = "SELECT UserID, FirstName + ' ' + LastName AS [Full Name], Email, Role FROM Users ORDER BY UserID DESC";
            dgv.DataSource = DBHelper.ExecuteQuery(query);
            ApplyGridColumns(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CreateUser(DataGridView dgv)
    {
        using CreateUserForm form = new CreateUserForm();
        if (form.ShowDialog() != DialogResult.OK) return;
        try
        {
            string query = "INSERT INTO Users (FirstName, LastName, Email, Password, Role) VALUES (@FirstName, @LastName, @Email, @Password, @Role)";
            SqlParameter[] p = {
                new("@FirstName", form.FirstName),
                new("@LastName",  form.LastName),
                new("@Email",     form.Email),
                new("@Password",  HashPassword(form.Password)),
                new("@Role",      form.Role)
            };
            DBHelper.ExecuteNonQuery(query, p);
            ShowToast("User created successfully.");
            LoadUsers(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void DeleteUser(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select a user to delete.")) return;
        string name = dgv.SelectedRows[0].Cells["Full Name"].Value?.ToString() ?? "this user";
        if (!Confirm($"Permanently delete {name}?")) return;
        try
        {
            int id = (int)dgv.SelectedRows[0].Cells["UserID"].Value;
            DBHelper.ExecuteNonQuery("DELETE FROM Users WHERE UserID = @UserID", new SqlParameter[] { new("@UserID", id) });
            ShowToast("User deleted.");
            LoadUsers(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ChangeUserRole(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select a user to change role.")) return;
        int id   = (int)dgv.SelectedRows[0].Cells["UserID"].Value;
        string role = PromptHelper.ShowChoice("Change User Role", "Select new role:", new[] { "Student", "SocietyHead", "Admin" });
        if (string.IsNullOrWhiteSpace(role)) return;
        try
        {
            DBHelper.ExecuteNonQuery("UPDATE Users SET Role = @Role WHERE UserID = @UserID",
                new SqlParameter[] { new("@Role", role), new("@UserID", id) });
            ShowToast($"Role updated to '{role}'.");
            LoadUsers(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void LoadSocieties(DataGridView dgv)
    {
        try
        {
            string query = @"SELECT s.SocietyID, s.Name, s.Status,
                                    u.FirstName + ' ' + u.LastName AS [Head],
                                    s.Description
                             FROM Societies s
                             LEFT JOIN Users u ON s.HeadID = u.UserID
                             WHERE s.Status <> 'Deleted'
                             ORDER BY s.Status, s.Name";
            dgv.DataSource = DBHelper.ExecuteQuery(query);
            ApplyGridColumns(dgv);
            ColorizeStatusColumn(dgv, "Status");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CreateSociety(DataGridView dgv)
    {
        string name = PromptHelper.ShowInput("Create Society", "Society Name:");
        if (string.IsNullOrWhiteSpace(name)) return;
        string description = PromptHelper.ShowInput("Create Society", "Description (optional):");
        string headEmail   = PromptHelper.ShowInput("Create Society", "Society Head Email:");
        if (string.IsNullOrWhiteSpace(headEmail)) return;
        try
        {
            object result = DBHelper.ExecuteScalar("SELECT UserID FROM Users WHERE Email = @Email",
                new SqlParameter[] { new("@Email", headEmail) });
            if (result == null) { ShowError("No user found with that email."); return; }
            DBHelper.ExecuteNonQuery(
                "INSERT INTO Societies (Name, Description, HeadID, Status) VALUES (@Name, @Description, @HeadID, 'Active')",
                new SqlParameter[] { new("@Name", name), new("@Description", description ?? ""), new("@HeadID", Convert.ToInt32(result)) });
            ShowToast("Society created and activated.");
            LoadSocieties(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ApproveSociety(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select a society to approve.")) return;
        int id = (int)dgv.SelectedRows[0].Cells["SocietyID"].Value;
        try
        {
            DBHelper.ExecuteNonQuery("UPDATE Societies SET Status = 'Active' WHERE SocietyID = @ID",
                new SqlParameter[] { new("@ID", id) });
            ShowToast("Society approved and set to Active.");
            LoadSocieties(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void SuspendSociety(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select a society to suspend.")) return;
        if (!Confirm("Suspend this society?")) return;
        int id = (int)dgv.SelectedRows[0].Cells["SocietyID"].Value;
        try
        {
            DBHelper.ExecuteNonQuery("UPDATE Societies SET Status = 'Suspended' WHERE SocietyID = @ID",
                new SqlParameter[] { new("@ID", id) });
            ShowToast("Society suspended.");
            LoadSocieties(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void DeleteSociety(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select a society to delete.")) return;
        if (!Confirm("Mark this society as deleted? This cannot be undone.")) return;
        int id = (int)dgv.SelectedRows[0].Cells["SocietyID"].Value;
        try
        {
            DBHelper.ExecuteNonQuery("UPDATE Societies SET Status = 'Deleted' WHERE SocietyID = @ID",
                new SqlParameter[] { new("@ID", id) });
            ShowToast("Society marked as deleted.");
            LoadSocieties(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void LoadEvents(DataGridView dgv)
    {
        try
        {
            string query = @"SELECT e.EventID, e.Title, s.Name AS Society,
                                    CONVERT(varchar, e.EventDate, 103) AS [Date],
                                    e.Location, e.Status
                             FROM Events e
                             JOIN Societies s ON e.SocietyID = s.SocietyID
                             ORDER BY e.EventDate DESC";
            dgv.DataSource = DBHelper.ExecuteQuery(query);
            ApplyGridColumns(dgv);
            ColorizeStatusColumn(dgv, "Status");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ApproveEvent(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select an event to approve.")) return;
        int id = (int)dgv.SelectedRows[0].Cells["EventID"].Value;
        try
        {
            DBHelper.ExecuteNonQuery("UPDATE Events SET Status = 'Approved' WHERE EventID = @ID",
                new SqlParameter[] { new("@ID", id) });
            ShowToast("Event approved.");
            LoadEvents(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CancelEvent(DataGridView dgv)
    {
        if (!HasSelection(dgv, "Select an event to cancel.")) return;
        if (!Confirm("Cancel this event?")) return;
        int id = (int)dgv.SelectedRows[0].Cells["EventID"].Value;
        try
        {
            DBHelper.ExecuteNonQuery("UPDATE Events SET Status = 'Cancelled' WHERE EventID = @ID",
                new SqlParameter[] { new("@ID", id) });
            ShowToast("Event cancelled.");
            LoadEvents(dgv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private string GenerateAdminReport()
    {
        int totalUsers         = SafeCount("SELECT COUNT(*) FROM Users");
        int adminUsers         = SafeCount("SELECT COUNT(*) FROM Users WHERE Role='Admin'");
        int studentUsers       = SafeCount("SELECT COUNT(*) FROM Users WHERE Role='Student'");
        int societyUsers       = SafeCount("SELECT COUNT(*) FROM Users WHERE Role='SocietyHead'");
        int totalSocieties     = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status <> 'Deleted'");
        int activeSocieties    = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status = 'Active'");
        int suspendedSocieties = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status = 'Suspended'");
        int totalEvents        = SafeCount("SELECT COUNT(*) FROM Events");
        int approvedEvents     = SafeCount("SELECT COUNT(*) FROM Events WHERE Status = 'Approved'");
        int pendingEvents      = SafeCount("SELECT COUNT(*) FROM Events WHERE Status = 'Pending'");
        int cancelledEvents    = SafeCount("SELECT COUNT(*) FROM Events WHERE Status = 'Cancelled'");
        int activeMembers      = SafeCount("SELECT COUNT(*) FROM Memberships WHERE Status = 'Approved'");
        int pendingMembers     = SafeCount("SELECT COUNT(*) FROM Memberships WHERE Status = 'Pending'");

        return
            $"╔══════════════════════════════════════════════════════╗\n" +
            $"║       UNIVERSITY SOCIETIES MANAGEMENT REPORT         ║\n" +
            $"║       Generated: {DateTime.Now:dd MMM yyyy  HH:mm:ss}               ║\n" +
            $"╠══════════════════════════════════════════════════════╣\n" +
            $"║  USERS                                               ║\n" +
            $"║    Total Registered Users    :  {totalUsers,-6}                ║\n" +
            $"║    Students                  :  {studentUsers,-6}                ║\n" +
            $"║    Society Heads             :  {societyUsers,-6}                ║\n" +
            $"║    Administrators            :  {adminUsers,-6}                ║\n" +
            $"╠══════════════════════════════════════════════════════╣\n" +
            $"║  SOCIETIES                                           ║\n" +
            $"║    Total (excl. deleted)     :  {totalSocieties,-6}                ║\n" +
            $"║    Active                    :  {activeSocieties,-6}                ║\n" +
            $"║    Suspended                 :  {suspendedSocieties,-6}                ║\n" +
            $"╠══════════════════════════════════════════════════════╣\n" +
            $"║  EVENTS                                              ║\n" +
            $"║    Total Events              :  {totalEvents,-6}                ║\n" +
            $"║    Approved                  :  {approvedEvents,-6}                ║\n" +
            $"║    Pending Review            :  {pendingEvents,-6}                ║\n" +
            $"║    Cancelled                 :  {cancelledEvents,-6}                ║\n" +
            $"╠══════════════════════════════════════════════════════╣\n" +
            $"║  MEMBERSHIPS                                         ║\n" +
            $"║    Active Memberships        :  {activeMembers,-6}                ║\n" +
            $"║    Pending Requests          :  {pendingMembers,-6}                ║\n" +
            $"╚══════════════════════════════════════════════════════╝";
    }

    // ─────────────────────────────────────────────
    //  UI Helpers
    // ─────────────────────────────────────────────
    private Panel SectionHeader(string title, string subtitle, Color accent)
    {
        Panel p = new Panel { BackColor = Color.Transparent, Height = 72, Width = 900, Location = new Point(0, 0) };
        Panel bar = new Panel { BackColor = accent, Size = new Size(4, 40), Location = new Point(0, 8) };
        Label lbl = new Label { Text = title, Font = FontTitle, ForeColor = TextPrimary, AutoSize = true, Location = new Point(16, 4) };
        Label sub = new Label { Text = subtitle, Font = FontBody, ForeColor = TextMuted, AutoSize = true, Location = new Point(16, 42) };
        p.Controls.AddRange(new Control[] { bar, lbl, sub });
        return p;
    }

    private Panel StatRow((string label, string value, Color accent)[] items, int cardW = 180)
    {
        int gap = 14, cardH = 88;
        Panel row = new Panel { BackColor = Color.Transparent, Height = cardH, Width = items.Length * (cardW + gap) };
        for (int i = 0; i < items.Length; i++)
        {
            var (label, value, accent) = items[i];
            Panel card = RoundCard(cardW, cardH, accent);
            card.Location = new Point(i * (cardW + gap), 0);

            Label valLbl = new Label { Text = value, Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = accent, AutoSize = true, Location = new Point(14, 10) };
            Label namLbl = new Label { Text = label, Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(14, 56) };
            Panel stripe = new Panel { BackColor = accent, Size = new Size(cardW, 3), Location = new Point(0, 0) };
            card.Controls.AddRange(new Control[] { stripe, valLbl, namLbl });
            row.Controls.Add(card);
        }
        return row;
    }

    private Panel RoundCard(int w, int h, Color accent)
    {
        Panel p = new Panel
        {
            Size      = new Size(w, h),
            BackColor = BgCard,
        };
        p.Paint += (s, e) => DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(60, accent.R, accent.G, accent.B));
        return p;
    }

    private Button ActionButton(string text, Color color, int w = 134)
    {
        Button btn = new Button
        {
            Text      = text,
            Size      = new Size(w, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, color.R, color.G, color.B),
            ForeColor = color,
            Font      = new Font("Segoe UI Semibold", 9f),
            Cursor    = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        btn.FlatAppearance.BorderColor         = Color.FromArgb(60, color.R, color.G, color.B);
        btn.FlatAppearance.BorderSize          = 1;
        btn.FlatAppearance.MouseOverBackColor  = Color.FromArgb(55, color.R, color.G, color.B);
        btn.FlatAppearance.MouseDownBackColor  = Color.FromArgb(75, color.R, color.G, color.B);
        return btn;
    }

    private DataGridView StyledGrid()
    {
        DataGridView dgv = new DataGridView
        {
            BackgroundColor          = BgCard,
            GridColor                = Border,
            BorderStyle              = BorderStyle.None,
            CellBorderStyle          = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode            = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect              = false,
            ReadOnly                 = true,
            AllowUserToAddRows       = false,
            AllowUserToDeleteRows    = false,
            AllowUserToResizeRows    = false,
            RowHeadersVisible        = false,
            AutoSizeColumnsMode      = DataGridViewAutoSizeColumnsMode.Fill,
            Font                     = FontBody,
            ForeColor                = TextPrimary,
            ScrollBars               = ScrollBars.Vertical,
            Anchor                   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight      = 40,
        };
        dgv.DefaultCellStyle.BackColor         = BgCard;
        dgv.DefaultCellStyle.ForeColor         = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor= Color.FromArgb(56, 139, 253, 50);
        dgv.DefaultCellStyle.SelectionForeColor= Color.White;
        dgv.DefaultCellStyle.Padding           = new Padding(4, 0, 4, 0);
        dgv.AlternatingRowsDefaultCellStyle.BackColor = BgHover;
        dgv.ColumnHeadersDefaultCellStyle.BackColor   = Color.FromArgb(17, 22, 30);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor   = TextMuted;
        dgv.ColumnHeadersDefaultCellStyle.Font        = new Font("Segoe UI Semibold", 9f);
        dgv.ColumnHeadersDefaultCellStyle.Padding     = new Padding(6, 0, 0, 0);
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowTemplate.Height = 36;
        return dgv;
    }

    private void ApplyGridColumns(DataGridView dgv)
    {
        // Hide ID columns from view but keep the data
        foreach (DataGridViewColumn col in dgv.Columns)
        {
            if (col.Name.EndsWith("ID"))
                col.Visible = false;
        }
    }

    private void ColorizeStatusColumn(DataGridView dgv, string colName)
    {
        dgv.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            string? header = dgv.Columns[e.ColumnIndex].HeaderText;
            if (header != colName) return;
            string? val = e.Value?.ToString();
            e.CellStyle.ForeColor = val switch
            {
                "Active"    or "Approved"  => AccentGreen,
                "Pending"                  => AccentOrange,
                "Suspended" or "Cancelled" => AccentRed,
                _                          => TextMuted,
            };
        };
    }

    private static Panel MakeDivider(DockStyle dock)
    {
        return new Panel { Dock = dock, Height = 1, BackColor = Border };
    }

    private static void DrawBorder(Graphics g, Rectangle r, Color c)
    {
        using Pen pen = new Pen(c);
        g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
    }

    private void SetActiveNav(int idx)
    {
        for (int i = 0; i < navButtons.Length; i++)
        {
            bool active = i == idx;
            navButtons[i].BackColor = active ? Color.FromArgb(20, AccentBlue.R, AccentBlue.G, AccentBlue.B) : Color.Transparent;
            navButtons[i].ForeColor = active ? AccentBlue : TextMuted;
        }
    }

    // ─────────────────────────────────────────────
    //  Utility Helpers
    // ─────────────────────────────────────────────
    private int SafeCount(string sql)
    {
        try { return Convert.ToInt32(DBHelper.ExecuteScalar(sql)); }
        catch { return 0; }
    }

    private bool HasSelection(DataGridView dgv, string msg)
    {
        if (dgv.SelectedRows.Count == 0) { ShowToast(msg); return false; }
        return true;
    }

    private bool Confirm(string msg)
        => MessageBox.Show(msg, "Confirm Action", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private void ShowToast(string msg)
        => MessageBox.Show(msg, "Admin Console", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowError(string msg)
        => MessageBox.Show($"Error: {msg}", "Admin Console", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private string HashPassword(string password)
    {
        using System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
    }
}