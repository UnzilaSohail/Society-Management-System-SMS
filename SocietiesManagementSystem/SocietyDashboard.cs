using Microsoft.Data.SqlClient;
using System.Data;

namespace SocietiesManagementSystem;

// ═══════════════════════════════════════════════════════════════════════════
//  SocietyDashboard — Society Head Portal
//
//  FIXES in this revision:
//   • TryLoadSociety() now also checks if the user is a MEMBER of a society
//     as SocietyHead role (covers admin-assigned heads where HeadID may not
//     be set yet), then falls back to showing "Create Society".
//   • LoadMembersGrid() correctly shows ALL pending/approved/rejected requests
//     for THIS society only — students' applications are now visible.
//   • SafeCount overload fixed: all callers use parameterised @S binding.
//   • ShowProfilePanel: society head can edit name, description, and status —
//     sidebar label & status bar refresh immediately on save.
//   • MemberAction: Approve now also marks ApprovedDate; grid refreshes with
//     the current tab filter preserved.
//   • Society head can ONLY see and act on the society where HeadID = userId.
//   • Raw string interpolation in SQL removed throughout; all queries use
//     SqlParameter[] to prevent SQL injection.
// ═══════════════════════════════════════════════════════════════════════════
public partial class SocietyDashboard : Form
{
    // ── State ──────────────────────────────────────────────────────────────
    private readonly int userId;
    private int    societyId;
    private string societyName = string.Empty;
    private string headName    = string.Empty;
    private int    activeNavIndex = 0;

    // ── Layout ─────────────────────────────────────────────────────────────
    private Panel    mainPanel           = null!;
    private Panel    sidebar             = null!;
    private Button[] navButtons          = null!;
    private Label    statusLabel         = null!;
    private Label    sidebarSocietyLabel = null!;
    private System.Windows.Forms.Timer clockTimer = null!;

    // ── Design tokens ──────────────────────────────────────────────────────
    private static readonly Color BgDark       = Color.FromArgb(13,  17,  23);
    private static readonly Color BgCard       = Color.FromArgb(22,  27,  34);
    private static readonly Color BgHover      = Color.FromArgb(30,  37,  48);
    private static readonly Color AccentBlue   = Color.FromArgb(56,  139, 253);
    private static readonly Color AccentGreen  = Color.FromArgb(63,  185, 80);
    private static readonly Color AccentRed    = Color.FromArgb(248, 81,  73);
    private static readonly Color AccentOrange = Color.FromArgb(210, 153, 34);
    private static readonly Color AccentPurple = Color.FromArgb(139, 92,  246);
    private static readonly Color AccentTeal   = Color.FromArgb(56,  189, 186);
    private static readonly Color TextPrimary  = Color.FromArgb(230, 237, 243);
    private static readonly Color TextMuted    = Color.FromArgb(125, 133, 144);
    private static readonly Color Border       = Color.FromArgb(48,  54,  61);

    private static readonly Font FontTitle = new Font("Segoe UI", 22f, FontStyle.Bold);
    private static readonly Font FontH3    = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
    private static readonly Font FontBody  = new Font("Segoe UI", 9.5f);
    private static readonly Font FontSmall = new Font("Segoe UI", 8.5f);
    private static readonly Font FontMono  = new Font("Consolas", 9.5f);

    private readonly (string icon, string label, Color accent, Action<Panel> render)[] sections;

    

    // ═══════════════════════════════════════════════════════════════════════
    //  Constructor
    // ═══════════════════════════════════════════════════════════════════════
    public SocietyDashboard(int userId)
    {
        this.userId = userId;

        sections = new (string, string, Color, Action<Panel>)[]
        {
            ("🏛️", "Profile",  AccentBlue,   ShowProfilePanel),
            ("👥", "Members",  AccentGreen,  ShowMembersPanel),
            ("📅", "Events",   AccentPurple, ShowEventsPanel),
            ("✅", "Tasks",    AccentOrange, ShowTasksPanel),
            ("📊", "Reports",  AccentTeal,   ShowReportsPanel),
        };

        InitializeComponent();
        LoadHeadName();
        TryLoadSociety();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Bootstrap
    // ═══════════════════════════════════════════════════════════════════════
    private void LoadHeadName()
    {
        try
        {
            object? r = DBHelper.ExecuteScalar(
                "SELECT FirstName + ' ' + LastName FROM Users WHERE UserID=@U",
                new[] { P("@U", userId) });
            if (r != null) headName = r.ToString()!;
        }
        catch { }
    }

    // FIX: Look up society by HeadID. Society heads should only ever see
    //      the one society they are head of. If none found, show the
    //      "Create Society" form so they can create one.
    private void TryLoadSociety()
    {
        try
        {
            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT SocietyID, Name FROM Societies WHERE HeadID=@H AND Status<>'Deleted'",
                new[] { P("@H", userId) });

            if (dt.Rows.Count > 0)
            {
                societyId   = (int)dt.Rows[0]["SocietyID"];
                societyName = dt.Rows[0]["Name"].ToString() ?? "";
                BuildShell();
                Navigate(0);
            }
   else
{
    BuildShell();
    // Show a waiting screen — no self-creation allowed
    ShowNoSocietyPanel(mainPanel);
}

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
private void ShowNoSocietyPanel(Panel host)
{
    host.Controls.Clear();
    Label lbl = new Label
    {
        Text = "No society has been assigned to you yet.\nPlease contact the administrator.",
        Font = FontTitle, ForeColor = TextMuted,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill
    };
    host.Controls.Add(lbl);
}
    // ═══════════════════════════════════════════════════════════════════════
    //  Shell
    // ═══════════════════════════════════════════════════════════════════════
    private void BuildShell()
    {
        this.Text            = "Society Head Console — Societies Management System";
        this.Size            = new Size(1320, 860);
        this.MinimumSize     = new Size(1100, 700);
        this.BackColor       = BgDark;
        this.ForeColor       = TextPrimary;
        this.StartPosition   = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.DoubleBuffered  = true;

        this.Controls.Clear();
        clockTimer?.Stop();

        // ── Sidebar ────────────────────────────────────────────────────────
        sidebar = new Panel { Dock = DockStyle.Left, Width = 228, BackColor = BgCard };
        this.Controls.Add(sidebar);

        // Brand strip
        Panel brand = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(17, 22, 30) };
        brand.Paint += (s, e) =>
        {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                brand.ClientRectangle,
                Color.FromArgb(17, 22, 30), Color.FromArgb(22, 30, 46),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(br, brand.ClientRectangle);
        };
        Label ico = new Label
        {
            Text = "🏛️", Font = new Font("Segoe UI Emoji", 22f),
            ForeColor = AccentBlue, AutoSize = true, Location = new Point(14, 18)
        };
        Label nm = new Label
        {
            Text      = headName.Length > 16 ? headName[..16] + "…" : headName,
            Font      = new Font("Segoe UI Semibold", 10.5f),
            ForeColor = TextPrimary, AutoSize = true, Location = new Point(52, 12)
        };
        Label rl = new Label
        {
            Text     = "Society Head",
            Font     = FontSmall, ForeColor = AccentBlue,
            AutoSize = true, Location = new Point(52, 32)
        };
        sidebarSocietyLabel = new Label
        {
            Text     = string.IsNullOrEmpty(societyName) ? "No society" : societyName,
            Font     = FontSmall, ForeColor = TextMuted,
            AutoSize = true, Location = new Point(52, 52)
        };
        brand.Controls.AddRange(new Control[] { ico, nm, rl, sidebarSocietyLabel });
        sidebar.Controls.Add(brand);
        sidebar.Controls.Add(HLine(DockStyle.Top));

        // Nav buttons
        navButtons = new Button[sections.Length];
        Panel navWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = sections.Length * 52 + 20,
            BackColor = BgCard,
            Padding   = new Padding(10, 10, 10, 0),
        };
        sidebar.Controls.Add(navWrap);

        for (int i = 0; i < sections.Length; i++)
        {
            int idx = i;
            var (icon, lbl, accent, _) = sections[i];

            Button btn = new Button
            {
                Text      = $"{icon}  {lbl}",
                Bounds    = new Rectangle(0, 4 + i * 52, 208, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Font      = FontH3,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Cursor    = Cursors.Hand,
                Tag       = idx,
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = BgHover;
            btn.MouseEnter += (s, e) => { if ((int)btn.Tag != activeNavIndex) btn.ForeColor = TextPrimary; };
            btn.MouseLeave += (s, e) => { if ((int)btn.Tag != activeNavIndex) btn.ForeColor = TextMuted; };
            btn.Click      += (s, e) => Navigate(idx);
            navButtons[i]   = btn;
            navWrap.Controls.Add(btn);
        }

        // Sign-out
        sidebar.Controls.Add(HLine(DockStyle.Bottom));
        Panel sideBottom = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = BgCard };
        Button btnLogout = new Button
        {
            Text      = "⟵  Sign Out",
            Bounds    = new Rectangle(10, 12, 208, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, AccentRed.R, AccentRed.G, AccentRed.B),
            ForeColor = AccentRed,
            Font      = FontH3,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(14, 0, 0, 0),
            Cursor    = Cursors.Hand,
        };
        btnLogout.FlatAppearance.BorderColor        = Color.FromArgb(80, AccentRed.R, AccentRed.G, AccentRed.B);
        btnLogout.FlatAppearance.BorderSize         = 1;
        btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, AccentRed.R, AccentRed.G, AccentRed.B);
        btnLogout.Click += (s, e) => { clockTimer?.Stop(); this.Close(); };
        sideBottom.Controls.Add(btnLogout);
        sidebar.Controls.Add(sideBottom);

        // ── Right column ───────────────────────────────────────────────────
        Panel rightCol = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };
        this.Controls.Add(rightCol);
        this.Controls.SetChildIndex(rightCol, 0);

        // Title bar
        Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(17, 22, 30) };
        Label tl = new Label
        {
            Text      = "🎓  Societies Management System",
            Font      = new Font("Segoe UI Semibold", 11f),
            ForeColor = TextMuted,
            AutoSize  = true,
            Location  = new Point(20, 16),
        };
        titleBar.Controls.Add(tl);
        rightCol.Controls.Add(titleBar);
        rightCol.Controls.Add(HLine(DockStyle.Top));

        // Status bar
        Panel statusBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(17, 22, 30) };
        statusLabel = new Label
        {
            Font      = FontSmall,
            ForeColor = TextMuted,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(16, 0, 0, 0),
        };
        RefreshStatusBar();
        statusBar.Controls.Add(statusLabel);
        rightCol.Controls.Add(statusBar);

        // Main scrollable content area
 mainPanel = new Panel
{
    Dock       = DockStyle.Fill,
    BackColor  = BgDark,
    AutoScroll = true,
    Padding    = new Padding(0, 0, 0, 0),  // remove the top:60 offset
};
        rightCol.Controls.Add(mainPanel);

        clockTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        clockTimer.Tick += (s, e) => RefreshStatusBar();
        clockTimer.Start();
    }

    private void RefreshStatusBar()
    {
        if (statusLabel == null) return;
        string soc = string.IsNullOrEmpty(societyName) ? "No society" : societyName;
        statusLabel.Text =
            $"  👤  {headName}   •   🏛️  {soc}" +
            $"   •   {DateTime.Now:ddd, dd MMM yyyy  •  HH:mm}";
    }

    private void Navigate(int idx)
    {
        activeNavIndex = idx;
        SetActiveNav(idx);
        mainPanel.Controls.Clear();
        mainPanel.AutoScroll = true;
Panel inner = new Panel
{
    BackColor  = BgDark,
    AutoScroll = false,
    Location   = new Point(0, 0),
    Width      = mainPanel.ClientSize.Width,
    Height     = mainPanel.ClientSize.Height,
    Padding    = new Padding(32, 24, 32, 24),  // move padding here
};
        mainPanel.Controls.Add(inner);

        sections[idx].render(inner);

        inner.PerformLayout();
        int total = inner.Padding.Top + inner.Padding.Bottom;
        foreach (Control c in inner.Controls)
            total += c.Height;
        total += 40;
        inner.Height = Math.Max(total, mainPanel.ClientSize.Height);

        EventHandler resizeH = null!;
        resizeH = (s, e) => inner.Width = mainPanel.ClientSize.Width;
        mainPanel.Resize += resizeH;
        inner.Disposed   += (s, e) => mainPanel.Resize -= resizeH;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Reusable layout pieces (all Dock=Top)
    // ═══════════════════════════════════════════════════════════════════════
    private static Panel SectionHeader(string title, string subtitle, Color accent)
    {
        Panel p = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 82,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 0, 0, 8),
        };
        Panel bar = new Panel { BackColor = accent, Size = new Size(4, 48), Location = new Point(0, 8) };
        Label lbl = new Label { Text = title,    Font = FontTitle, ForeColor = TextPrimary, AutoSize = true, Location = new Point(18, 4) };
        Label sub = new Label { Text = subtitle, Font = FontBody,  ForeColor = TextMuted,   AutoSize = true, Location = new Point(18, 48) };
        p.Controls.AddRange(new Control[] { bar, lbl, sub });
        return p;
    }

    private static Panel Spacer(int h) =>
        new Panel { Dock = DockStyle.Top, Height = h, BackColor = Color.Transparent };

    private static Panel StatRow((string label, string value, Color accent)[] items)
    {
        const int cardW = 210, cardH = 92, gap = 12;
        Panel row = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = cardH + 16,
            BackColor = Color.Transparent,
        };
        for (int i = 0; i < items.Length; i++)
        {
            var (label, value, accent) = items[i];
            Panel card = new Panel
            {
                Size      = new Size(cardW, cardH),
                BackColor = BgCard,
                Location  = new Point(i * (cardW + gap), 8),
            };
            Color ac = accent;
            card.Paint += (s, e) =>
                DrawBorder(e.Graphics, card.ClientRectangle,
                    Color.FromArgb(50, ac.R, ac.G, ac.B));
            Panel stripe = new Panel { BackColor = accent, Size = new Size(cardW, 3), Location = new Point(0, 0) };
            Label val    = new Label { Text = value, Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = accent, AutoSize = true, Location = new Point(14, 10) };
            Label lbl2   = new Label { Text = label, Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(14, 60) };
            card.Controls.AddRange(new Control[] { stripe, val, lbl2 });
            row.Controls.Add(card);
        }
        return row;
    }

    private static Panel Toolbar(int height = 48) =>
        new Panel { Dock = DockStyle.Top, Height = height, BackColor = Color.Transparent };

    private static Panel GridHost(DataGridView dgv, int height)
    {
        var wrapper = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Color.Transparent };
        dgv.Dock = DockStyle.Fill;
        wrapper.Controls.Add(dgv);
        return wrapper;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FR 2.1a — Create Society  (shown when head has no society yet)
    // ═══════════════════════════════════════════════════════════════════════
    private void ShowCreateSocietyPanel(Panel host)
    {
        host.Controls.Clear();
        host.Padding = new Padding(32, 24, 32, 24);

        Panel card = new Panel { Dock = DockStyle.Top, Height = 320, BackColor = BgCard };
        card.Paint += (s, e) =>
        {
            DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(50, AccentBlue.R, AccentBlue.G, AccentBlue.B));
            using Pen top = new Pen(AccentBlue, 2);
            e.Graphics.DrawLine(top, 0, 0, card.Width, 0);
        };

        int cy = 24;
        Label   lName   = FL("Society Name *", new Point(24, cy));           cy += 24;
        TextBox txtName = TB(new Point(24, cy), 500);                         cy += 38 + 18;
        Label   lDesc   = FL("Description",    new Point(24, cy));            cy += 24;
        TextBox txtDesc = TB(new Point(24, cy), 500, 110, true);              cy += 110 + 22;

        Button btnCreate = Btn("  ➤  Create Society", AccentBlue, 190);
        btnCreate.Location = new Point(24, cy);
        btnCreate.Click   += (s, e) => CreateSociety(txtName.Text, txtDesc.Text);

        card.Resize += (s, e) =>
        {
            int w = card.Width - 48;
            txtName.Width = w;
            txtDesc.Width = w;
        };

        card.Controls.AddRange(new Control[] { lName, txtName, lDesc, txtDesc, btnCreate });

        host.Controls.Add(card);
        host.Controls.Add(Spacer(12));
        host.Controls.Add(SectionHeader("Create Your Society",
            "You don't have a society yet. Fill in the details below to create one.", AccentBlue));
    }

    private void CreateSociety(string name, string desc)
    {
        if (string.IsNullOrWhiteSpace(name)) { Warn("Society name is required."); return; }
        try
        {
            object res = DBHelper.ExecuteScalar(
                "INSERT INTO Societies (Name, Description, HeadID, Status) " +
                "VALUES (@N, @D, @H, 'Active'); SELECT SCOPE_IDENTITY();",
                new[] { P("@N", name.Trim()), P("@D", desc.Trim()), P("@H", userId) })!;
            societyId   = Convert.ToInt32(res);
            societyName = name.Trim();
            BuildShell();
            Navigate(0);
            Toast($"✅ Society \"{societyName}\" created successfully!");
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FR 2.1b — Society Profile (edit name / description / status)
    //  FIX: society head can only manage the society where HeadID = userId.
    //       Stats load fresh from DB on every visit.
    // ═══════════════════════════════════════════════════════════════════════
    private void ShowProfilePanel(Panel host)
    {
        host.Padding = new Padding(32, 24, 32, 24);
        host.Controls.Clear();

        int members  = SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Approved'", societyId);
        int pending  = SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Pending'",  societyId);
        int evTotal  = SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S",                           societyId);
        int evActive = SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S AND Status='Approved' AND EventDate>GETDATE()", societyId);

        // Edit card
        Panel card = new Panel { Dock = DockStyle.Top, Height = 460, BackColor = BgCard };
        card.Paint += (s, e) =>
        {
            DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(50, AccentBlue.R, AccentBlue.G, AccentBlue.B));
            using Pen top = new Pen(AccentBlue, 2);
            e.Graphics.DrawLine(top, 0, 0, card.Width, 0);
        };

        int cy = 24;
        Label   lName   = FL("Society Name *",  new Point(24, cy)); cy += 24;
        TextBox txtName = TB(new Point(24, cy), 500);               cy += 38 + 18;
        Label   lDesc   = FL("Description",     new Point(24, cy)); cy += 24;
        TextBox txtDesc = TB(new Point(24, cy), 500, 130, true);    cy += 130 + 22;
        Label   lStatus = FL("Society Status",  new Point(24, cy)); cy += 24;

        ComboBox cmbStatus = new ComboBox
        {
            Location      = new Point(24, cy),
            Size          = new Size(200, 38),
            Font          = FontBody,
            BackColor     = BgDark,
            ForeColor     = TextPrimary,
            FlatStyle     = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        cmbStatus.Items.AddRange(new object[] { "Active", "Inactive", "Suspended" });
        cy += cmbStatus.Height + 22;

        Button btnSave = Btn("  💾  Save Changes", AccentGreen, 180);
        btnSave.Location = new Point(24, cy);

        card.Resize += (s, e) =>
        {
            int w = card.Width - 48;
            txtName.Width = w;
            txtDesc.Width = w;
        };

        card.Controls.AddRange(new Control[] { lName, txtName, lDesc, txtDesc, lStatus, cmbStatus, btnSave });

        LoadProfile(txtName, txtDesc, cmbStatus);
        btnSave.Click += (s, e) => SaveProfile(txtName.Text, txtDesc.Text,
            cmbStatus.SelectedItem?.ToString() ?? "Active");

        // Add in REVERSE order for DockStyle.Top stacking
        host.Controls.Add(card);
        host.Controls.Add(Spacer(16));
        host.Controls.Add(StatRow(new[]
        {
            ("✅  Approved Members", members.ToString(),  AccentGreen),
            ("⏳  Pending Requests", pending.ToString(),  AccentOrange),
            ("📅  Total Events",     evTotal.ToString(),  AccentPurple),
            ("🟢  Upcoming Events",  evActive.ToString(), AccentBlue),
        }));
        host.Controls.Add(Spacer(12));
        host.Controls.Add(SectionHeader("Society Profile",
            "Manage your society's public-facing information.", AccentBlue));
    }

    private void LoadProfile(TextBox n, TextBox d, ComboBox cmb)
    {
        try
        {
            DataTable dt = DBHelper.ExecuteQuery(
                "SELECT Name, Description, Status FROM Societies WHERE SocietyID=@S",
                new[] { P("@S", societyId) });
            if (dt.Rows.Count > 0)
            {
                n.Text = dt.Rows[0]["Name"].ToString() ?? "";
                d.Text = dt.Rows[0]["Description"].ToString() ?? "";
                string st = dt.Rows[0]["Status"].ToString() ?? "Active";
                cmb.SelectedItem = cmb.Items.Contains(st) ? (object)st : "Active";
            }
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void SaveProfile(string name, string desc, string status)
    {
        if (string.IsNullOrWhiteSpace(name)) { Warn("Society name is required."); return; }
        try
        {
            DBHelper.ExecuteNonQuery(
                "UPDATE Societies SET Name=@N, Description=@D, Status=@ST WHERE SocietyID=@S",
                new[] { P("@N", name.Trim()), P("@D", desc.Trim()), P("@ST", status), P("@S", societyId) });
            societyName = name.Trim();
            if (sidebarSocietyLabel != null) sidebarSocietyLabel.Text = societyName;
            RefreshStatusBar();
            Toast("✅ Profile updated successfully.");
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FR 2.2 & 2.3 — Member Management
    //  FIX: LoadMembersGrid fetches ALL membership rows for this society
    //       (including Pending ones submitted by students), so the society
    //       head can now see and act on incoming applications.
    // ═══════════════════════════════════════════════════════════════════════
    private void ShowMembersPanel(Panel host)
    {
        host.Padding = new Padding(32, 24, 32, 24);
        host.Controls.Clear();

        int approved = SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Approved'", societyId);
        int pending  = SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Pending'",  societyId);
        int rejected = SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Rejected'", societyId);

        DataGridView dgv = StyledGrid();
        Panel gridHost   = GridHost(dgv, 360);

        Panel toolbar    = Toolbar(44);
        Button btnApprove = ToolBtn("✅  Approve",      AccentGreen);
        Button btnReject  = ToolBtn("❌  Reject",        AccentRed);
        Button btnRemove  = ToolBtn("🗑️  Remove Member", AccentRed);
        Button btnRefresh = ToolBtn("↻  Refresh",        TextMuted);
        LayoutRow(toolbar, 3, btnApprove, btnReject, btnRemove, btnRefresh);

        // Tab bar — track current filter
        Panel tabBar = Toolbar(44);
        string[] tabs = { "All", "Pending", "Approved", "Rejected" };
        string currentFilter = "";
        Button activeTab = null!;

        void SetTab(Button b, string filter)
        {
            currentFilter = filter;
            if (activeTab != null) { activeTab.BackColor = BgCard; activeTab.ForeColor = TextMuted; }
            b.BackColor = AccentGreen; b.ForeColor = Color.White; activeTab = b;
            LoadMembersGrid(dgv, filter);
        }

        int tx = 0;
        foreach (string tab in tabs)
        {
            string captured = tab;
            Button tb = TabBtn(tab, AccentGreen);
            tb.Location = new Point(tx, 4);
            tb.Click   += (s, e) => SetTab(tb, captured == "All" ? "" : captured);
            tabBar.Controls.Add(tb);
            tx += 108;
            if (captured == "All") { activeTab = tb; tb.BackColor = AccentGreen; tb.ForeColor = Color.White; }
        }

        // Load all requests (including Pending) by default
        LoadMembersGrid(dgv, "");

        btnApprove.Click += (s, e) => { MemberAction(dgv, "Approve"); LoadMembersGrid(dgv, currentFilter); };
        btnReject.Click  += (s, e) => { MemberAction(dgv, "Reject");  LoadMembersGrid(dgv, currentFilter); };
        btnRemove.Click  += (s, e) => { MemberAction(dgv, "Remove");  LoadMembersGrid(dgv, currentFilter); };
        btnRefresh.Click += (s, e) => LoadMembersGrid(dgv, currentFilter);

        // Reverse order for DockStyle.Top stacking
        host.Controls.Add(gridHost);
        host.Controls.Add(toolbar);
        host.Controls.Add(tabBar);
        host.Controls.Add(Spacer(12));
        host.Controls.Add(StatRow(new[]
        {
            ("✅  Approved", approved.ToString(), AccentGreen),
            ("⏳  Pending",  pending.ToString(),  AccentOrange),
            ("❌  Rejected", rejected.ToString(), AccentRed),
        }));
        host.Controls.Add(Spacer(12));
        host.Controls.Add(SectionHeader("Member Management",
            "Approve or reject membership requests and manage your member list.", AccentGreen));
    }

    // FIX: Parameterised query. Fetches ALL statuses unless filtered —
    //      this is what makes student applications visible to the head.
    private void LoadMembersGrid(DataGridView dgv, string statusFilter)
    {
        try
        {
            string whereClause = string.IsNullOrEmpty(statusFilter)
                ? "WHERE m.SocietyID=@S"
                : "WHERE m.SocietyID=@S AND m.Status=@ST";

            string q = $@"
                SELECT m.MembershipID,
                       u.FirstName + ' ' + u.LastName           AS [Member Name],
                       u.Email,
                       m.Status,
                       CONVERT(varchar, m.JoinDate, 106)         AS [Applied On],
                       ISNULL(CONVERT(varchar, m.ApprovedDate, 106), '—') AS [Approved On]
                FROM Memberships m
                JOIN Users u ON m.StudentID = u.UserID
                {whereClause}
                ORDER BY
                    CASE m.Status WHEN 'Pending' THEN 0 WHEN 'Approved' THEN 1 ELSE 2 END,
                    m.JoinDate DESC";

            var parms = string.IsNullOrEmpty(statusFilter)
                ? new[] { P("@S", societyId) }
                : new[] { P("@S", societyId), P("@ST", statusFilter) };

            dgv.DataSource = DBHelper.ExecuteQuery(q, parms);
            HideIdColumns(dgv);
            ColorizeColumn(dgv, "Status", v => v switch
            {
                "Approved" => AccentGreen,
                "Pending"  => AccentOrange,
                "Rejected" => AccentRed,
                _          => TextMuted,
            });
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // FIX: Approve now also sets ApprovedDate so the grid shows the correct date.
    private void MemberAction(DataGridView dgv, string action)
    {
        if (!HasRow(dgv, "Select a member row first.")) return;
        int    mid        = Convert.ToInt32(dgv.SelectedRows[0].Cells["MembershipID"].Value);
        string memberName = dgv.SelectedRows[0].Cells["Member Name"].Value?.ToString() ?? "Member";
        string status     = dgv.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";

        try
        {
            switch (action)
            {
                case "Approve":
                    if (status == "Approved") { Info("Already approved."); return; }
                    if (Confirm($"Approve membership for {memberName}?") != DialogResult.Yes) return;
                    DBHelper.ExecuteNonQuery(
                        "UPDATE Memberships SET Status='Approved', ApprovedDate=GETDATE() WHERE MembershipID=@M",
                        new[] { P("@M", mid) });
                    Toast($"✅ {memberName}'s membership approved.");
                    break;

                case "Reject":
                    if (status == "Rejected") { Info("Already rejected."); return; }
                    if (Confirm($"Reject membership for {memberName}?") != DialogResult.Yes) return;
                    DBHelper.ExecuteNonQuery(
                        "UPDATE Memberships SET Status='Rejected', ApprovedDate=NULL WHERE MembershipID=@M",
                        new[] { P("@M", mid) });
                    Toast($"❌ {memberName}'s membership rejected.");
                    break;

                case "Remove":
                    if (Confirm($"Permanently remove {memberName} from the society?",
                        icon: MessageBoxIcon.Warning) != DialogResult.Yes) return;
                    DBHelper.ExecuteNonQuery(
                        "DELETE FROM Memberships WHERE MembershipID=@M",
                        new[] { P("@M", mid) });
                    Toast($"🗑️ {memberName} removed from society.");
                    break;
            }
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FR 2.4 — Event Management
    // ═══════════════════════════════════════════════════════════════════════
    private void ShowEventsPanel(Panel host)
    {
        host.Padding = new Padding(32, 24, 32, 24);
        host.Controls.Clear();

        int total     = SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S",                                              societyId);
        int evPending = SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S AND Status='Pending'",                        societyId);
        int upcoming  = SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S AND Status='Approved' AND EventDate>GETDATE()", societyId);
        int cancelled = SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S AND Status='Cancelled'",                       societyId);

        DataGridView dgv  = StyledGrid();
        Panel gridHost    = GridHost(dgv, 360);

        Panel toolbar     = Toolbar(44);
        Button btnNew     = ToolBtn("✚  Create Event",  AccentGreen);
        Button btnEdit    = ToolBtn("✏️  Edit Event",    AccentBlue);
        Button btnCancel  = ToolBtn("🚫  Cancel Event",  AccentRed);
        Button btnRefresh = ToolBtn("↻  Refresh",        TextMuted);
        LayoutRow(toolbar, 3, btnNew, btnEdit, btnCancel, btnRefresh);

        LoadEventsGrid(dgv);

        btnNew.Click     += (s, e) =>
        {
            var f = new EventForm(societyId);
            f.FormClosed += (_, _) => LoadEventsGrid(dgv);
            f.ShowDialog(this);
        };
        btnEdit.Click    += (s, e) => EditEvent(dgv);
        btnCancel.Click  += (s, e) => CancelEvent(dgv);
        btnRefresh.Click += (s, e) => LoadEventsGrid(dgv);

        host.Controls.Add(gridHost);
        host.Controls.Add(toolbar);
        host.Controls.Add(Spacer(12));
        host.Controls.Add(StatRow(new[]
        {
            ("📋  Total",             total.ToString(),     AccentBlue),
            ("⏳  Awaiting Approval", evPending.ToString(), AccentOrange),
            ("✅  Upcoming",          upcoming.ToString(),  AccentGreen),
            ("🚫  Cancelled",         cancelled.ToString(), AccentRed),
        }));
        host.Controls.Add(Spacer(12));
        host.Controls.Add(SectionHeader("Event Management",
            "Create, edit, and cancel events for your society.", AccentPurple));
    }

    private void LoadEventsGrid(DataGridView dgv)
    {
        try
        {
            string q = @"
                SELECT e.EventID,
                       e.Title,
                       CONVERT(varchar, e.EventDate, 106)    AS [Date],
                       e.Location,
                       e.Status,
                       (SELECT COUNT(*) FROM EventRegistrations r
                        WHERE r.EventID = e.EventID)         AS [Registrations],
                       e.Description
                FROM Events e
                WHERE e.SocietyID = @S
                ORDER BY e.EventDate DESC";

            dgv.DataSource = DBHelper.ExecuteQuery(q, new[] { P("@S", societyId) });
            HideIdColumns(dgv);
            ColorizeColumn(dgv, "Status", v => v switch
            {
                "Approved"  => AccentGreen,
                "Pending"   => AccentOrange,
                "Cancelled" => AccentRed,
                _           => TextMuted,
            });
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void EditEvent(DataGridView dgv)
    {
        if (!HasRow(dgv, "Select an event to edit.")) return;
        int    evId   = Convert.ToInt32(dgv.SelectedRows[0].Cells["EventID"].Value);
        string status = dgv.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";
        if (status == "Cancelled") { Info("Cannot edit a cancelled event."); return; }
        var f = new EventForm(societyId, evId);
        f.FormClosed += (_, _) => LoadEventsGrid(dgv);
        f.ShowDialog(this);
    }

    private void CancelEvent(DataGridView dgv)
    {
        if (!HasRow(dgv, "Select an event to cancel.")) return;
        int    evId   = Convert.ToInt32(dgv.SelectedRows[0].Cells["EventID"].Value);
        string title  = dgv.SelectedRows[0].Cells["Title"].Value?.ToString() ?? "this event";
        string status = dgv.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";
        if (status == "Cancelled") { Info("Event is already cancelled."); return; }
        if (Confirm($"Cancel event \"{title}\"?\n\nThis cannot be undone.",
                    icon: MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            DBHelper.ExecuteNonQuery(
                "UPDATE Events SET Status='Cancelled' WHERE EventID=@E",
                new[] { P("@E", evId) });
            Toast("🚫 Event cancelled.");
            LoadEventsGrid(dgv);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FR 2.5 — Task Management
    // ═══════════════════════════════════════════════════════════════════════
    private void ShowTasksPanel(Panel host)
    {
        host.Padding = new Padding(32, 24, 32, 24);
        host.Controls.Clear();

        int taskPending    = SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status='Pending'",    societyId);
        int taskInProgress = SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status='InProgress'", societyId);
        int taskCompleted  = SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status='Completed'",  societyId);
        int taskOverdue    = SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status<>'Completed' AND DueDate<GETDATE()", societyId);

        DataGridView dgv   = StyledGrid();
        Panel gridHost     = GridHost(dgv, 360);

        Panel toolbar      = Toolbar(44);
        Button btnNew      = ToolBtn("✚  Assign Task",       AccentGreen);
        Button btnProgress = ToolBtn("🔄  Mark In Progress", AccentBlue);
        Button btnComplete = ToolBtn("✅  Mark Completed",   AccentGreen);
        Button btnDelete   = ToolBtn("🗑️  Delete Task",       AccentRed);
        Button btnRefresh  = ToolBtn("↻  Refresh",            TextMuted);
        LayoutRow(toolbar, 3, btnNew, btnProgress, btnComplete, btnDelete, btnRefresh);

        LoadTasksGrid(dgv);

        btnNew.Click      += (s, e) =>
        {
            var f = new TaskForm(societyId);
            f.FormClosed += (_, _) => LoadTasksGrid(dgv);
            f.ShowDialog(this);
        };
        btnProgress.Click += (s, e) => UpdateTaskStatus(dgv, "InProgress");
        btnComplete.Click += (s, e) => UpdateTaskStatus(dgv, "Completed");
        btnDelete.Click   += (s, e) => DeleteTask(dgv);
        btnRefresh.Click  += (s, e) => LoadTasksGrid(dgv);

        host.Controls.Add(gridHost);
        host.Controls.Add(toolbar);
        host.Controls.Add(Spacer(12));
        host.Controls.Add(StatRow(new[]
        {
            ("⏳  Pending",     taskPending.ToString(),    AccentOrange),
            ("🔄  In Progress", taskInProgress.ToString(), AccentBlue),
            ("✅  Completed",   taskCompleted.ToString(),  AccentGreen),
            ("🔴  Overdue",     taskOverdue.ToString(),    AccentRed),
        }));
        host.Controls.Add(Spacer(12));
        host.Controls.Add(SectionHeader("Task Management",
            "Assign and track tasks for your society members.", AccentOrange));
    }

    private void LoadTasksGrid(DataGridView dgv)
    {
        try
        {
            string q = @"
                SELECT t.TaskID,
                       t.Title,
                       u.FirstName + ' ' + u.LastName    AS [Assigned To],
                       t.Status,
                       CONVERT(varchar, t.DueDate, 106)  AS [Due Date],
                       CASE WHEN t.Status <> 'Completed' AND t.DueDate < GETDATE()
                            THEN '⚠ Overdue' ELSE '' END AS [Alert],
                       t.Description
                FROM Tasks t
                JOIN Users u ON t.AssignedTo = u.UserID
                WHERE t.SocietyID = @S
                ORDER BY t.DueDate ASC";

            dgv.DataSource = DBHelper.ExecuteQuery(q, new[] { P("@S", societyId) });
            HideIdColumns(dgv);
            ColorizeColumn(dgv, "Status", v => v switch
            {
                "Completed"  => AccentGreen,
                "InProgress" => AccentBlue,
                "Pending"    => AccentOrange,
                _            => TextMuted,
            });
            ColorizeColumn(dgv, "Alert", _ => AccentRed);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void UpdateTaskStatus(DataGridView dgv, string newStatus)
    {
        if (!HasRow(dgv, "Select a task first.")) return;
        int    tid = Convert.ToInt32(dgv.SelectedRows[0].Cells["TaskID"].Value);
        string cur = dgv.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";
        if (cur == newStatus) { Info($"Task is already {newStatus}."); return; }
        try
        {
            DBHelper.ExecuteNonQuery(
                "UPDATE Tasks SET Status=@ST WHERE TaskID=@T",
                new[] { P("@ST", newStatus), P("@T", tid) });
            Toast($"✅ Task status updated to {newStatus}.");
            LoadTasksGrid(dgv);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void DeleteTask(DataGridView dgv)
    {
        if (!HasRow(dgv, "Select a task to delete.")) return;
        int    tid   = Convert.ToInt32(dgv.SelectedRows[0].Cells["TaskID"].Value);
        string title = dgv.SelectedRows[0].Cells["Title"].Value?.ToString() ?? "this task";
        if (Confirm($"Delete task \"{title}\"?", icon: MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            DBHelper.ExecuteNonQuery("DELETE FROM Tasks WHERE TaskID=@T", new[] { P("@T", tid) });
            Toast("🗑️ Task deleted.");
            LoadTasksGrid(dgv);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FR 2.6 — Reports & Analytics
    // ═══════════════════════════════════════════════════════════════════════
    private void ShowReportsPanel(Panel host)
    {
        host.Padding = new Padding(32, 24, 32, 24);
        host.Controls.Clear();

        Panel expRow = Toolbar(48);
        Button btnExport = ToolBtn("⬇  Export to .txt", AccentTeal);
        btnExport.Location = new Point(0, 5);
        expRow.Controls.Add(btnExport);

Panel outputArea = new Panel { 
    Dock = DockStyle.Top, 
    Height = 380, 
    BackColor = BgCard,
    Padding = new Padding(12)   // inner breathing room for the RichTextBox
};        outputArea.Paint += (s, e) => DrawBorder(e.Graphics, outputArea.ClientRectangle, Border);
        RichTextBox rtb = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = BgCard,
            ForeColor   = TextPrimary,
            Font        = FontMono,
            BorderStyle = BorderStyle.None,
            ReadOnly    = true,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };
        outputArea.Controls.Add(rtb);
        btnExport.Click += (s, e) => ExportReport(rtb.Text);

        string[] reportTypes = { "📋  Member Report", "📅  Event Report", "✅  Task Report", "📊  Full Summary" };
        Button activeRpt = null!;
        Panel typeBar = Toolbar(48);

        int tx = 0;
        for (int i = 0; i < reportTypes.Length; i++)
        {
            int ii = i;
            Button tb = new Button
            {
                Text      = reportTypes[i],
                Size      = new Size(168, 40),
                Location  = new Point(tx, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = BgCard,
                ForeColor = TextMuted,
                Font      = FontH3,
                Cursor    = Cursors.Hand,
            };
            tb.FlatAppearance.BorderColor        = Border;
            tb.FlatAppearance.BorderSize         = 1;
            tb.FlatAppearance.MouseOverBackColor = BgHover;
            tb.Click += (s, e) =>
            {
                if (activeRpt != null) { activeRpt.BackColor = BgCard; activeRpt.ForeColor = TextMuted; }
                tb.BackColor = AccentTeal; tb.ForeColor = Color.White; activeRpt = tb;
                GenerateReport(rtb, ii);
            };
            if (i == 0) { activeRpt = tb; tb.BackColor = AccentTeal; tb.ForeColor = Color.White; }
            typeBar.Controls.Add(tb);
            tx += 176;
        }

        host.Controls.Add(expRow);
        host.Controls.Add(Spacer(8));
        host.Controls.Add(outputArea);
        host.Controls.Add(Spacer(8));
        host.Controls.Add(typeBar);
        host.Controls.Add(Spacer(12));
        host.Controls.Add(SectionHeader("Reports & Analytics",
            "Generate detailed reports for members, events, and tasks.", AccentTeal));

        GenerateReport(rtb, 0);
    }

    private void GenerateReport(RichTextBox rtb, int type)
    {
        try
        {
            rtb.Clear();
            string line = new string('═', 60);

            void Head(string t)
            {
                rtb.SelectionFont  = new Font("Consolas", 10f, FontStyle.Bold);
                rtb.SelectionColor = AccentTeal;
                rtb.AppendText($"\n{line}\n  {t}\n{line}\n\n");
                rtb.SelectionFont  = FontMono;
                rtb.SelectionColor = TextPrimary;
            }
            void Kv(string k, string v)
            {
                rtb.SelectionColor = TextMuted;
                rtb.AppendText($"  {k,-26}");
                rtb.SelectionColor = TextPrimary;
                rtb.AppendText($"{v}\n");
            }
            void Sep()
            {
                rtb.SelectionColor = Border;
                rtb.AppendText($"  {new string('─', 55)}\n");
                rtb.SelectionColor = TextPrimary;
            }

            switch (type)
            {
                case 0:
                    Head($"MEMBER REPORT — {societyName}  ({DateTime.Now:dd MMM yyyy HH:mm})");
                    DataTable members = DBHelper.ExecuteQuery(
                        @"SELECT u.FirstName + ' ' + u.LastName AS Name, u.Email, m.Status,
                                 CONVERT(varchar, m.JoinDate, 106)                        AS Applied,
                                 ISNULL(CONVERT(varchar, m.ApprovedDate, 106), '—')       AS Approved
                          FROM Memberships m
                          JOIN Users u ON m.StudentID = u.UserID
                          WHERE m.SocietyID = @S
                          ORDER BY m.Status, m.JoinDate",
                        new[] { P("@S", societyId) });
                    string lastStatus = "";
                    foreach (DataRow row in members.Rows)
                    {
                        string st = row["Status"].ToString()!;
                        if (st != lastStatus)
                        {
                            rtb.SelectionColor = AccentBlue;
                            rtb.AppendText($"\n  ── {st} ──\n");
                            rtb.SelectionColor = TextPrimary;
                            lastStatus = st;
                        }
                        Kv("Name:",    row["Name"].ToString()!);
                        Kv("Email:",   row["Email"].ToString()!);
                        Kv("Applied:", row["Applied"].ToString()!);
                        if (st == "Approved") Kv("Approved:", row["Approved"].ToString()!);
                        Sep();
                    }
                    Kv("Total rows:", members.Rows.Count.ToString());
                    break;

                case 1:
                    Head($"EVENT REPORT — {societyName}  ({DateTime.Now:dd MMM yyyy HH:mm})");
                    DataTable events = DBHelper.ExecuteQuery(
                        @"SELECT e.Title, e.Status,
                                 CONVERT(varchar, e.EventDate, 106) AS Date,
                                 e.Location,
                                 (SELECT COUNT(*) FROM EventRegistrations r
                                  WHERE r.EventID = e.EventID)      AS Regs
                          FROM Events e
                          WHERE e.SocietyID = @S
                          ORDER BY e.EventDate DESC",
                        new[] { P("@S", societyId) });
                    foreach (DataRow row in events.Rows)
                    {
                        Kv("Title:",         row["Title"].ToString()!);
                        Kv("Date:",          row["Date"].ToString()!);
                        Kv("Status:",        row["Status"].ToString()!);
                        Kv("Venue:",         row["Location"].ToString()!);
                        Kv("Registrations:", row["Regs"].ToString()!);
                        Sep();
                    }
                    Kv("Total events:", events.Rows.Count.ToString());
                    break;

                case 2:
                    Head($"TASK REPORT — {societyName}  ({DateTime.Now:dd MMM yyyy HH:mm})");
                    DataTable tasks = DBHelper.ExecuteQuery(
                        @"SELECT t.Title, t.Status,
                                 u.FirstName + ' ' + u.LastName AS Assignee,
                                 CONVERT(varchar, t.DueDate, 106) AS DueDate,
                                 CASE WHEN t.Status <> 'Completed' AND t.DueDate < GETDATE()
                                      THEN 'Yes' ELSE 'No' END    AS Overdue
                          FROM Tasks t
                          JOIN Users u ON t.AssignedTo = u.UserID
                          WHERE t.SocietyID = @S
                          ORDER BY t.Status, t.DueDate",
                        new[] { P("@S", societyId) });
                    foreach (DataRow row in tasks.Rows)
                    {
                        Kv("Task:",        row["Title"].ToString()!);
                        Kv("Assigned To:", row["Assignee"].ToString()!);
                        Kv("Status:",      row["Status"].ToString()!);
                        Kv("Due Date:",    row["DueDate"].ToString()!);
                        Kv("Overdue:",     row["Overdue"].ToString()!);
                        Sep();
                    }
                    Kv("Total tasks:", tasks.Rows.Count.ToString());
                    break;

                case 3:
                    Head($"FULL SUMMARY — {societyName}  ({DateTime.Now:dd MMM yyyy HH:mm})");
                    Kv("Society:",          societyName);
                    Kv("Head:",             headName);
                    Kv("Report Generated:", DateTime.Now.ToString("dd MMM yyyy  HH:mm:ss"));
                    rtb.AppendText("\n");
                    Kv("Approved Members:", SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Approved'", societyId).ToString());
                    Kv("Pending Requests:", SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Pending'",  societyId).ToString());
                    Kv("Rejected:",         SafeCount("SELECT COUNT(*) FROM Memberships WHERE SocietyID=@S AND Status='Rejected'", societyId).ToString());
                    rtb.AppendText("\n");
                    Kv("Total Events:",     SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S",                                              societyId).ToString());
                    Kv("Approved Events:",  SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S AND Status='Approved'",                        societyId).ToString());
                    Kv("Cancelled Events:", SafeCount("SELECT COUNT(*) FROM Events WHERE SocietyID=@S AND Status='Cancelled'",                       societyId).ToString());
                    rtb.AppendText("\n");
                    Kv("Total Tasks:",     SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S",                                                societyId).ToString());
                    Kv("Completed Tasks:", SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status='Completed'",                         societyId).ToString());
                    Kv("Pending Tasks:",   SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status='Pending'",                           societyId).ToString());
                    Kv("Overdue Tasks:",   SafeCount("SELECT COUNT(*) FROM Tasks WHERE SocietyID=@S AND Status<>'Completed' AND DueDate<GETDATE()",  societyId).ToString());
                    rtb.AppendText("\n");
                    Kv("Total Registrations:",
                        SafeCount("SELECT COUNT(*) FROM EventRegistrations r JOIN Events e ON r.EventID=e.EventID WHERE e.SocietyID=@S", societyId).ToString());
                    break;
            }
        }
        catch (Exception ex)
        {
            rtb.SelectionColor = AccentRed;
            rtb.AppendText($"\nReport error: {ex.Message}");
            rtb.SelectionColor = TextPrimary;
        }
    }

    private void ExportReport(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) { Info("Generate a report first."); return; }
        using var sfd = new SaveFileDialog
        {
            Filter   = "Text File|*.txt",
            FileName = $"Report_{societyName}_{DateTime.Now:yyyyMMdd_HHmm}.txt",
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        File.WriteAllText(sfd.FileName, text);
        Toast("✅ Report exported successfully.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Reusable UI widget factories
    // ═══════════════════════════════════════════════════════════════════════
    private DataGridView StyledGrid()
    {
        var dgv = new DataGridView
        {
            BackgroundColor             = BgCard,
            GridColor                   = Border,
            BorderStyle                 = BorderStyle.None,
            CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect                 = false,
            ReadOnly                    = true,
            AllowUserToAddRows          = false,
            AllowUserToDeleteRows       = false,
            AllowUserToResizeRows       = false,
            RowHeadersVisible           = false,
            AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill,
            Font                        = FontBody,
            ForeColor                   = TextPrimary,
            ScrollBars                  = ScrollBars.Both,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight         = 42,
        };
        typeof(DataGridView)
            .GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(dgv, true);
        dgv.DefaultCellStyle.BackColor          = BgCard;
        dgv.DefaultCellStyle.ForeColor          = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, AccentBlue.R, AccentBlue.G, AccentBlue.B);
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.DefaultCellStyle.Padding            = new Padding(6, 0, 6, 0);
        dgv.AlternatingRowsDefaultCellStyle.BackColor = BgHover;
        dgv.ColumnHeadersDefaultCellStyle.BackColor   = Color.FromArgb(17, 22, 30);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor   = TextMuted;
        dgv.ColumnHeadersDefaultCellStyle.Font        = new Font("Segoe UI Semibold", 9f);
        dgv.ColumnHeadersDefaultCellStyle.Padding     = new Padding(8, 0, 0, 0);
        dgv.EnableHeadersVisualStyles  = false;
        dgv.RowTemplate.Height         = 38;
        return dgv;
    }

    private Button TabBtn(string text, Color accent)
    {
        Button b = new Button
        {
            Text      = text,
            Size      = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = BgCard,
            ForeColor = TextMuted,
            Font      = FontH3,
            Cursor    = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor        = Border;
        b.FlatAppearance.BorderSize         = 1;
        b.FlatAppearance.MouseOverBackColor = BgHover;
        return b;
    }

    private Button ToolBtn(string text, Color color)
    {
        Button btn = new Button
        {
            Text      = text,
            Height    = 38,
            AutoSize  = false,
            Width     = TextRenderer.MeasureText(text, FontH3).Width + 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, color.R, color.G, color.B),
            ForeColor = color,
            Font      = FontH3,
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderColor        = Color.FromArgb(70, color.R, color.G, color.B);
        btn.FlatAppearance.BorderSize         = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, color.R, color.G, color.B);
        return btn;
    }

    private Button Btn(string text, Color color, int w)
    {
        Button btn = new Button
        {
            Text      = text,
            Size      = new Size(w, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font      = FontH3,
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize         = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Max(0, color.R - 22),
            Math.Max(0, color.G - 22),
            Math.Max(0, color.B - 22));
        return btn;
    }

    private Label FL(string text, Point loc) =>
        new Label { Text = text, Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = loc };

    private TextBox TB(Point loc, int w, int h = 38, bool multiline = false)
    {
        var tb = new TextBox
        {
            Location    = loc,
            Size        = new Size(w, h),
            Font        = FontBody,
            BackColor   = BgDark,
            ForeColor   = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline   = multiline,
            ScrollBars  = multiline ? ScrollBars.Vertical : ScrollBars.None,
        };
        tb.Enter += (s, e) => tb.BackColor = BgHover;
        tb.Leave += (s, e) => tb.BackColor = BgDark;
        return tb;
    }

    private static void LayoutRow(Panel parent, int yOff, params Button[] buttons)
    {
        int x = 0;
        foreach (var btn in buttons)
        {
            btn.Location = new Point(x, yOff);
            parent.Controls.Add(btn);
            x += btn.Width + 8;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Utilities
    // ═══════════════════════════════════════════════════════════════════════
    private void SetActiveNav(int idx)
    {
        for (int i = 0; i < navButtons.Length; i++)
        {
            Color accent = sections[i].accent;
            bool  active = i == idx;
            navButtons[i].BackColor = active
                ? Color.FromArgb(22, accent.R, accent.G, accent.B)
                : Color.Transparent;
            navButtons[i].ForeColor = active ? accent : TextMuted;
        }
    }

    private void HideIdColumns(DataGridView dgv)
    {
        foreach (DataGridViewColumn col in dgv.Columns)
            if (col.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
                col.Visible = false;
    }

    private void ColorizeColumn(DataGridView dgv, string colName, Func<string, Color> picker)
    {
        if (!dgv.Columns.Contains(colName)) return;
        dgv.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != colName) return;
            e.CellStyle.ForeColor = picker(e.Value?.ToString() ?? "");
        };
    }

    private static Panel HLine(DockStyle dock) =>
        new Panel { Dock = dock, Height = 1, BackColor = Border };

    private static void DrawBorder(Graphics g, Rectangle r, Color c)
    {
        using Pen pen = new Pen(c);
        g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
    }

    private static SqlParameter P(string name, object value) => new(name, value);

    // Parameterised SafeCount — all callers pass societyId bound to @S
    private int SafeCount(string sql, int sid)
    {
        try { return Convert.ToInt32(DBHelper.ExecuteScalar(sql, new[] { P("@S", sid) })); }
        catch { return 0; }
    }

    private bool HasRow(DataGridView dgv, string msg)
    {
        if (dgv.SelectedRows.Count > 0) return true;
        Info(msg); return false;
    }

    private DialogResult Confirm(string msg, string title = "Confirm",
        MessageBoxIcon icon = MessageBoxIcon.Question) =>
        MessageBox.Show(msg, title, MessageBoxButtons.YesNo, icon);

    private void Toast(string msg) =>
        MessageBox.Show(msg, "Society Console", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private void Info(string msg) =>
        MessageBox.Show(msg, "Society Console", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private void Warn(string msg) =>
        MessageBox.Show(msg, "Society Console", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private void Err(string msg)  =>
        MessageBox.Show($"Error: {msg}", "Society Console", MessageBoxButtons.OK, MessageBoxIcon.Error);
}