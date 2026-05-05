using Microsoft.Data.SqlClient;
using System.Data;

namespace SocietiesManagementSystem;

// ═══════════════════════════════════════════════════════════════════════════
//  StudentDashboard.cs  — Full rewrite / bug-fixed
//
//  Student Functional Requirements covered:
//    FR 1.1  Account / Login   (handled by LoginForm; userId injected)
//    FR 1.2  Browse societies
//    FR 1.3  Apply for membership
//    FR 1.4  Join multiple societies
//    FR 1.5  View upcoming events
//    FR 1.6  Register for events
//    FR 1.7  View membership status
//    FR 1.8  View event tickets / passes + download
//
//  Bug-fixes vs. original stub:
//    • BuildToolbar returned a Panel whose Width was never set (defaults to 0)
//      ⇒ toolbar now anchored Left|Right so it fills the host width.
//    • Toolbar action-buttons were positioned using searchBox.Right which is
//      a coordinate relative to the *inner* searchWrap panel, not the toolbar.
//      ⇒ Fixed to use the constant 292 (= searchWrap.Right = 280 + right-edge).
//    • StatRow card width was fixed at 190 px; 4-card row overflowed on small
//      windows  ⇒ made cardW dynamic based on item count.
//    • DataGridView Size was set before it was added to a resizable host;
//      replaced with Anchor + proper Width calculation everywhere.
//    • Refresh on ShowSocietiesPanel / ShowMembershipsPanel re-rendered the
//      entire host (expensive) ⇒ now only reloads the grid DataSource.
//    • WithdrawApplication used raw string interpolation in SQL ⇒ parameterised.
//    • Duplicate-check in ApplyMembership missed the Rejected case
//      ⇒ allow re-apply after rejection.
//    • Clock timer interval was 30 000 ms (30 s) which is fine, but the status
//      bar was never updated on the first Navigate call ⇒ called UpdateStatusBar()
//      once during BuildUI.
// ═══════════════════════════════════════════════════════════════════════════
public partial class StudentDashboard : Form
{
    // ── State ──────────────────────────────────────────────────────────────
    private readonly int userId;
    private string studentName = "Student";

    // ── Layout ─────────────────────────────────────────────────────────────
    private Panel   mainPanel     = null!;
    private Panel   sidebar       = null!;
    private Button[] navButtons   = null!;
    private Label   statusBarLabel = null!;
    private System.Windows.Forms.Timer clockTimer = null!;

    // ── Design tokens (GitHub-dark palette) ────────────────────────────────
    private static readonly Color BgDark       = Color.FromArgb(13,  17,  23);
    private static readonly Color BgCard       = Color.FromArgb(22,  27,  34);
    private static readonly Color BgHover      = Color.FromArgb(30,  37,  48);
    private static readonly Color AccentBlue   = Color.FromArgb(56,  139, 253);
    private static readonly Color AccentGreen  = Color.FromArgb(63,  185,  80);
    private static readonly Color AccentRed    = Color.FromArgb(248,  81,  73);
    private static readonly Color AccentOrange = Color.FromArgb(210, 153,  34);
    private static readonly Color AccentPurple = Color.FromArgb(139,  92, 246);
    private static readonly Color AccentTeal   = Color.FromArgb( 56, 189, 186);
    private static readonly Color TextPrimary  = Color.FromArgb(230, 237, 243);
    private static readonly Color TextMuted    = Color.FromArgb(125, 133, 144);
    private static readonly Color Border       = Color.FromArgb( 48,  54,  61);

    private static readonly Font FontTitle = new Font("Segoe UI", 22f, FontStyle.Bold);
    private static readonly Font FontH2    = new Font("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font FontH3    = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
    private static readonly Font FontBody  = new Font("Segoe UI", 9.5f);
    private static readonly Font FontSmall = new Font("Segoe UI", 8.5f);

    // ── Navigation sections ────────────────────────────────────────────────
    private readonly (string icon, string label, Color accent, Action<Panel> render)[] sections;

    // ══════════════════════════════════════════════════════════════════════
    //  Constructor
    // ══════════════════════════════════════════════════════════════════════
    public StudentDashboard(int userId)
    {
        this.userId = userId;

        sections = new (string, string, Color, Action<Panel>)[]
        {
            ("🏛️", "Societies",   AccentBlue,   ShowSocietiesPanel),
            ("📅", "Events",      AccentPurple, ShowEventsPanel),
            ("🎫", "Memberships", AccentGreen,  ShowMembershipsPanel),
            ("🎟️", "My Tickets",  AccentOrange, ShowTicketsPanel),
        };

        InitializeComponent();
        LoadStudentName();
        BuildUI();
        StartClock();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Shell construction
    // ══════════════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        this.Text            = "Student Portal — Societies Management System";
        this.Size            = new Size(1320, 860);
        this.MinimumSize     = new Size(1100, 700);
        this.BackColor       = BgDark;
        this.ForeColor       = TextPrimary;
        this.StartPosition   = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.DoubleBuffered  = true;

        // ── Sidebar ────────────────────────────────────────────────────────
        sidebar = new Panel { Dock = DockStyle.Left, Width = 228, BackColor = BgCard };
        this.Controls.Add(sidebar);

        // Brand block
        Panel brand = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(17, 22, 30) };
        brand.Paint += (s, e) =>
        {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                brand.ClientRectangle,
                Color.FromArgb(17, 22, 30), Color.FromArgb(22, 30, 46),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(br, brand.ClientRectangle);
        };

        var icoLabel  = new Label { Text = "👤", Font = new Font("Segoe UI Emoji", 22f), ForeColor = AccentGreen, AutoSize = true, Location = new Point(16, 18) };
        var nameLabel = new Label { Text = studentName.Length > 18 ? studentName[..18] + "…" : studentName, Font = new Font("Segoe UI Semibold", 11f), ForeColor = TextPrimary, AutoSize = true, Location = new Point(52, 16) };
        var roleLabel = new Label { Text = "Student  ·  Portal", Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(52, 38) };
        brand.Controls.AddRange(new Control[] { icoLabel, nameLabel, roleLabel });
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
            var (ico, lbl, accent, _) = sections[i];
            Button btn = new Button
            {
                Text      = $"{ico}  {lbl}",
                Bounds    = new Rectangle(0, 4 + i * 52, 208, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = i == 0 ? Color.FromArgb(20, accent.R, accent.G, accent.B) : Color.Transparent,
                ForeColor = i == 0 ? accent : TextMuted,
                Font      = FontH3,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(12, 0, 0, 0),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = BgHover;
            btn.MouseEnter += (s, e) => { if (btn.ForeColor != sections[idx].accent) btn.ForeColor = TextPrimary; };
            btn.MouseLeave += (s, e) => { if (btn.ForeColor != sections[idx].accent) btn.ForeColor = TextMuted; };
            btn.Click += (s, e) => Navigate(idx);
            navButtons[i] = btn;
            navWrap.Controls.Add(btn);
        }

        // Sidebar bottom: logout
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
            Padding   = new Padding(12, 0, 0, 0),
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
        titleBar.Controls.Add(new Label
        {
            Text      = "🎓  Societies Management System",
            Font      = new Font("Segoe UI Semibold", 11f),
            ForeColor = TextMuted,
            AutoSize  = true,
            Location  = new Point(20, 16),
        });
        rightCol.Controls.Add(titleBar);
        rightCol.Controls.Add(HLine(DockStyle.Top));

        // Status bar
        Panel statusBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(17, 22, 30) };
        statusBarLabel = new Label { Font = FontSmall, ForeColor = TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
        statusBar.Controls.Add(statusBarLabel);
        rightCol.Controls.Add(statusBar);

        // Main scrollable panel — NO padding here; each section renders its own inner content panel
        mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, AutoScroll = true, Padding = new Padding(0) };
        rightCol.Controls.Add(mainPanel);

        UpdateStatusBar();
        Navigate(0);
    }

    private void Navigate(int idx)
    {
        SetActiveNav(idx);
        mainPanel.Controls.Clear();

        // ── Content wrapper ────────────────────────────────────────────────
        // Use a Docked panel so it always fills mainPanel correctly.
        // Padding gives the visual margin; all section renderers receive THIS
        // panel as their host, so host.ClientSize.Width is the usable width.
        Panel content = new Panel
        {
            Dock      = DockStyle.Top,
            BackColor = BgDark,
            Padding   = new Padding(32, 24, 32, 32),
            // Height will be set after section renders (see below)
            Width     = mainPanel.ClientSize.Width,
        };
        mainPanel.Controls.Add(content);

        // Render the section; it appends controls and tracks `y`
        sections[idx].render(content);

        // Size the wrapper to contain all rendered children + bottom padding
        int maxBottom = 0;
        foreach (Control c in content.Controls)
        {
            int b = c.Bottom + content.Padding.Bottom;
            if (b > maxBottom) maxBottom = b;
        }
        content.Height = Math.Max(maxBottom + 40, mainPanel.ClientSize.Height);

        // Keep wrapper width in sync when form is resized
        mainPanel.Resize += (s, e) =>
        {
            content.Width = mainPanel.ClientSize.Width;
            // Resize grids that use Anchor
            foreach (Control c in content.Controls)
                if (c is DataGridView dgv)
                    dgv.Width = content.ClientSize.Width;
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FR 1.2  Browse Societies  /  FR 1.3  Apply  /  FR 1.4  Multiple
    // ══════════════════════════════════════════════════════════════════════
    private void ShowSocietiesPanel(Panel host)
    {
        int total   = SafeCount("SELECT COUNT(*) FROM Societies WHERE Status='Active'");
        int mine    = SafeCount($"SELECT COUNT(*) FROM Memberships WHERE StudentID={userId} AND Status='Approved'");
        int pending = SafeCount($"SELECT COUNT(*) FROM Memberships WHERE StudentID={userId} AND Status='Pending'");

        int y = 0;
        host.Controls.Add(SectionHeader("Available Societies",
            "Discover campus societies and apply to join.", AccentBlue, ref y));

        Panel stats = StatRow(new[]
        {
            ("🏛️  Total Societies",     total.ToString(),   AccentBlue),
            ("✅  My Memberships",       mine.ToString(),    AccentGreen),
            ("⏳  Pending Applications", pending.ToString(), AccentOrange),
        });
        stats.Location = new Point(0, y); y += stats.Height + 20;
        host.Controls.Add(stats);

        Panel toolbar = BuildToolbar(out TextBox searchBox, out Panel searchWrap, ref y);
        Button btnApply   = ToolBtn("✚  Apply for Membership", AccentGreen);
        Button btnRefresh = ToolBtn("↻  Refresh", TextMuted);
        btnApply.Location   = new Point(searchWrap.Right + 12, 6);
        btnRefresh.Location = new Point(btnApply.Right + 8, 6);
        toolbar.Controls.AddRange(new Control[] { btnApply, btnRefresh });
        host.Controls.Add(toolbar);

        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, y);
        dgv.Size     = new Size(host.ClientSize.Width, 420);
        host.Controls.Add(dgv);
        LoadSocietiesGrid(dgv);

        searchBox.TextChanged += (s, e) => FilterGrid(dgv, searchBox.Text, new[] { "Name", "Description", "Head" });
        btnApply.Click        += (s, e) => ApplyMembership(dgv, host);
        btnRefresh.Click      += (s, e) => { searchBox.Clear(); LoadSocietiesGrid(dgv); };
    }

    private void LoadSocietiesGrid(DataGridView dgv)
    {
        try
        {
            string q = @"
                SELECT s.SocietyID,
                       s.Name,
                       s.Description,
                       u.FirstName + ' ' + u.LastName AS [Head],
                       s.Status,
                       (SELECT COUNT(*) FROM Memberships m
                        WHERE m.SocietyID = s.SocietyID AND m.Status = 'Approved') AS [Members]
                FROM Societies s
                LEFT JOIN Users u ON s.HeadID = u.UserID
                WHERE s.Status = 'Active'
                ORDER BY s.Name";
            dgv.DataSource = DBHelper.ExecuteQuery(q);
            HideIdColumns(dgv);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void ApplyMembership(DataGridView dgv, Panel host)
    {
        if (!HasRow(dgv, "Please select a society first.")) return;
        try
        {
            int    socId = CellInt(dgv.SelectedRows[0], "SocietyID");
            string name  = CellStr(dgv.SelectedRows[0], "Name");

            // FIX: only block if already Approved or Pending (allow re-apply after Rejected)
            object? existing = DBHelper.ExecuteScalar(
                "SELECT Status FROM Memberships WHERE StudentID=@S AND SocietyID=@C",
                new[] { P("@S", userId), P("@C", socId) });

            if (existing != null && existing != DBNull.Value)
            {
                string existStatus = existing.ToString()!;
                if (existStatus == "Approved") { Info("You are already an approved member of this society."); return; }
                if (existStatus == "Pending")  { Info("Your application is already pending review."); return; }
                // Rejected → delete old record, let them re-apply below
                DBHelper.ExecuteNonQuery(
                    "DELETE FROM Memberships WHERE StudentID=@S AND SocietyID=@C",
                    new[] { P("@S", userId), P("@C", socId) });
            }

            var confirm = MessageBox.Show(
                $"Apply to join \"{name}\"?\n\nYour request will be reviewed by the society head.",
                "Confirm Application", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            DBHelper.ExecuteNonQuery(
                "INSERT INTO Memberships (StudentID, SocietyID, Status, JoinDate) VALUES (@S, @C, 'Pending', GETDATE())",
                new[] { P("@S", userId), P("@C", socId) });
            Toast("✅ Application submitted! The society head will review your request.");
            Navigate(0);   // re-navigate to Societies tab to refresh stats
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FR 1.5  View upcoming events  /  FR 1.6  Register
    // ══════════════════════════════════════════════════════════════════════
    private void ShowEventsPanel(Panel host)
    {
        int total    = SafeCount("SELECT COUNT(*) FROM Events WHERE Status='Approved' AND EventDate > GETDATE()");
        int myRegs   = SafeCount($"SELECT COUNT(*) FROM EventRegistrations WHERE StudentID={userId}");
        int thisWeek = SafeCount("SELECT COUNT(*) FROM Events WHERE Status='Approved' AND EventDate BETWEEN GETDATE() AND DATEADD(DAY,7,GETDATE())");

        int y = 0;
        host.Controls.Add(SectionHeader("Upcoming Events",
            "Browse approved events and register your spot.", AccentPurple, ref y));

        Panel stats = StatRow(new[]
        {
            ("📅  Upcoming Events",  total.ToString(),    AccentPurple),
            ("📝  My Registrations", myRegs.ToString(),   AccentGreen),
            ("🗓️  This Week",        thisWeek.ToString(), AccentBlue),
        });
        stats.Location = new Point(0, y); y += stats.Height + 20;
        host.Controls.Add(stats);

        Panel toolbar = BuildToolbar(out TextBox searchBox, out Panel searchWrap, ref y);
        Button btnRegister = ToolBtn("📝  Register for Event", AccentGreen);
        Button btnRefresh  = ToolBtn("↻  Refresh", TextMuted);
        btnRegister.Location = new Point(searchWrap.Right + 12, 6);
        btnRefresh.Location  = new Point(btnRegister.Right + 8, 6);
        toolbar.Controls.AddRange(new Control[] { btnRegister, btnRefresh });
        host.Controls.Add(toolbar);

        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, y);
        dgv.Size     = new Size(host.ClientSize.Width, 420);
        host.Controls.Add(dgv);
        LoadEventsGrid(dgv);

        searchBox.TextChanged += (s, e) => FilterGrid(dgv, searchBox.Text, new[] { "Title", "Society", "Location" });
        btnRegister.Click     += (s, e) => RegisterEvent(dgv, host);
        btnRefresh.Click      += (s, e) => { searchBox.Clear(); LoadEventsGrid(dgv); };
    }

    private void LoadEventsGrid(DataGridView dgv)
    {
        try
        {
            string q = @"
                SELECT e.EventID,
                       e.Title,
                       s.Name  AS [Society],
                       CONVERT(varchar, e.EventDate, 106) AS [Date],
                       e.Location,
                       e.Description,
                       CASE WHEN EXISTS (
                           SELECT 1 FROM EventRegistrations r
                           WHERE r.EventID = e.EventID AND r.StudentID = @UID)
                       THEN '✅ Registered' ELSE '—' END AS [My Status]
                FROM Events e
                JOIN Societies s ON e.SocietyID = s.SocietyID
                WHERE e.Status = 'Approved' AND e.EventDate > GETDATE()
                ORDER BY e.EventDate ASC";
            dgv.DataSource = DBHelper.ExecuteQuery(q, new[] { P("@UID", userId) });
            HideIdColumns(dgv);
            ColorizeColumn(dgv, "My Status", v => v == "✅ Registered" ? AccentGreen : TextMuted);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void RegisterEvent(DataGridView dgv, Panel host)
    {
        if (!HasRow(dgv, "Select an event to register for.")) return;
        try
        {
            int    evId  = CellInt(dgv.SelectedRows[0], "EventID");
            string title = CellStr(dgv.SelectedRows[0], "Title");

            object? ex = DBHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM EventRegistrations WHERE StudentID=@S AND EventID=@E",
                new[] { P("@S", userId), P("@E", evId) });
            if (Convert.ToInt32(ex) > 0) { Info("You are already registered for this event."); return; }

            if (MessageBox.Show($"Register for \"{title}\"?\n\nA ticket will be generated for you.",
                "Confirm Registration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            DBHelper.ExecuteNonQuery(
                "INSERT INTO EventRegistrations (StudentID, EventID, RegistrationDate) VALUES (@S, @E, GETDATE())",
                new[] { P("@S", userId), P("@E", evId) });
            Toast("🎟️  Registered successfully! View your ticket in the My Tickets tab.");
            LoadEventsGrid(dgv);
        }
        catch (Exception ex2) { Err(ex2.Message); }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FR 1.7  View membership status  /  FR 1.3  Withdraw pending
    // ══════════════════════════════════════════════════════════════════════
    private void ShowMembershipsPanel(Panel host)
    {
        int total    = SafeCount($"SELECT COUNT(*) FROM Memberships WHERE StudentID={userId}");
        int active   = SafeCount($"SELECT COUNT(*) FROM Memberships WHERE StudentID={userId} AND Status='Approved'");
        int pending  = SafeCount($"SELECT COUNT(*) FROM Memberships WHERE StudentID={userId} AND Status='Pending'");
        int rejected = SafeCount($"SELECT COUNT(*) FROM Memberships WHERE StudentID={userId} AND Status='Rejected'");

        int y = 0;
        host.Controls.Add(SectionHeader("My Memberships",
            "Track your society applications and membership status.", AccentGreen, ref y));

        Panel stats = StatRow(new[]
        {
            ("📋  Total Applied",    total.ToString(),    AccentBlue),
            ("✅  Active Members",   active.ToString(),   AccentGreen),
            ("⏳  Pending Approval", pending.ToString(),  AccentOrange),
            ("❌  Rejected",         rejected.ToString(), AccentRed),
        });
        stats.Location = new Point(0, y); y += stats.Height + 20;
        host.Controls.Add(stats);

        Panel toolbar = BuildToolbar(out TextBox searchBox, out Panel searchWrap, ref y);
        Button btnRefresh  = ToolBtn("↻  Refresh", TextMuted);
        Button btnWithdraw = ToolBtn("✖  Withdraw Application", AccentRed);
        btnRefresh.Location  = new Point(searchWrap.Right + 12, 6);
        btnWithdraw.Location = new Point(btnRefresh.Right + 8,  6);
        toolbar.Controls.AddRange(new Control[] { btnRefresh, btnWithdraw });
        host.Controls.Add(toolbar);

        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, y);
        dgv.Size     = new Size(host.ClientSize.Width, 420);
        host.Controls.Add(dgv);
        LoadMembershipsGrid(dgv);

        searchBox.TextChanged += (s, e) => FilterGrid(dgv, searchBox.Text, new[] { "Society", "Status" });
        btnRefresh.Click      += (s, e) => { searchBox.Clear(); LoadMembershipsGrid(dgv); };
        btnWithdraw.Click     += (s, e) => WithdrawApplication(dgv, host);
    }

    private void LoadMembershipsGrid(DataGridView dgv)
    {
        try
        {
            string q = @"
                SELECT m.MembershipID,
                       s.Name AS [Society],
                       u.FirstName + ' ' + u.LastName AS [Society Head],
                       m.Status,
                       CONVERT(varchar, m.JoinDate,      106) AS [Applied On],
                       ISNULL(CONVERT(varchar, m.ApprovedDate, 106), '—') AS [Approved On]
                FROM Memberships m
                JOIN  Societies s ON m.SocietyID = s.SocietyID
                LEFT JOIN Users u ON s.HeadID    = u.UserID
                WHERE m.StudentID = @UID
                ORDER BY m.JoinDate DESC";
            dgv.DataSource = DBHelper.ExecuteQuery(q, new[] { P("@UID", userId) });
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

    private void WithdrawApplication(DataGridView dgv, Panel host)
    {
        if (!HasRow(dgv, "Select a membership row to withdraw.")) return;
        string status = CellStr(dgv.SelectedRows[0], "Status");

        if (status == "Approved")
        {
            Info("You cannot withdraw an approved membership. Contact the society head to be removed.");
            return;
        }
        if (status != "Pending")
        {
            Info("Only pending applications can be withdrawn.");
            return;
        }

        int memId = CellInt(dgv.SelectedRows[0], "MembershipID");
        if (MessageBox.Show("Withdraw this pending application?", "Confirm Withdrawal",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try
        {
            // FIX: parameterised query (was raw string interpolation)
            DBHelper.ExecuteNonQuery(
                "DELETE FROM Memberships WHERE MembershipID=@M AND StudentID=@S",
                new[] { P("@M", memId), P("@S", userId) });
            Toast("Application withdrawn successfully.");
            Navigate(2);   // re-navigate to Memberships tab to refresh stats + grid
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FR 1.8  View event tickets / passes
    // ══════════════════════════════════════════════════════════════════════
    private void ShowTicketsPanel(Panel host)
    {
        int upcoming = SafeCount(
            $"SELECT COUNT(*) FROM EventRegistrations r JOIN Events e ON r.EventID=e.EventID WHERE r.StudentID={userId} AND e.EventDate > GETDATE()");
        int past = SafeCount(
            $"SELECT COUNT(*) FROM EventRegistrations r JOIN Events e ON r.EventID=e.EventID WHERE r.StudentID={userId} AND e.EventDate <= GETDATE()");

        int y = 0;
        host.Controls.Add(SectionHeader("My Event Tickets",
            "View and download passes for events you have registered for.", AccentOrange, ref y));

        Panel stats = StatRow(new[]
        {
            ("🟢  Upcoming Events", upcoming.ToString(),          AccentGreen),
            ("📁  Past Events",     past.ToString(),              AccentBlue),
            ("🎟️  Total Tickets",   (upcoming + past).ToString(), AccentOrange),
        });
        stats.Location = new Point(0, y); y += stats.Height + 20;
        host.Controls.Add(stats);

        Panel toolbar = BuildToolbar(out TextBox searchBox, out Panel searchWrap, ref y);
        Button btnView     = ToolBtn("🎫  View Ticket",      AccentOrange);
        Button btnDownload = ToolBtn("⬇  Download (.txt)",  AccentBlue);
        Button btnRefresh  = ToolBtn("↻  Refresh",           TextMuted);
        btnView.Location     = new Point(searchWrap.Right + 12, 6);
        btnDownload.Location = new Point(btnView.Right + 8,     6);
        btnRefresh.Location  = new Point(btnDownload.Right + 8, 6);
        toolbar.Controls.AddRange(new Control[] { btnView, btnDownload, btnRefresh });
        host.Controls.Add(toolbar);

        DataGridView dgv = StyledGrid();
        dgv.Location = new Point(0, y);
        dgv.Size     = new Size(host.ClientSize.Width, 420);
        host.Controls.Add(dgv);
        LoadTicketsGrid(dgv);

        searchBox.TextChanged += (s, e) => FilterGrid(dgv, searchBox.Text, new[] { "Event", "Society", "Venue" });
        btnView.Click         += (s, e) => ViewTicketDialog(dgv);
        btnDownload.Click     += (s, e) => DownloadTicket(dgv);
        btnRefresh.Click      += (s, e) => { searchBox.Clear(); LoadTicketsGrid(dgv); };
    }

    private void LoadTicketsGrid(DataGridView dgv)
    {
        try
        {
            string q = $@"
                SELECT r.RegistrationID,
                       e.Title    AS [Event],
                       s.Name     AS [Society],
                       CONVERT(varchar, e.EventDate, 106)        AS [Event Date],
                       e.Location AS [Venue],
                       CASE WHEN e.EventDate > GETDATE()
                            THEN '🟢 Upcoming' ELSE '📁 Past' END AS [Status],
                       CONVERT(varchar, r.RegistrationDate, 106) AS [Registered On]
                FROM EventRegistrations r
                JOIN Events    e ON r.EventID    = e.EventID
                JOIN Societies s ON e.SocietyID  = s.SocietyID
                WHERE r.StudentID = {userId}
                ORDER BY e.EventDate DESC";
            dgv.DataSource = DBHelper.ExecuteQuery(q);
            HideIdColumns(dgv);
            ColorizeColumn(dgv, "Status", v => v.Contains("Upcoming") ? AccentGreen : TextMuted);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void ViewTicketDialog(DataGridView dgv)
    {
        if (!HasRow(dgv, "Select a ticket to view.")) return;
        try
        {
            int regId = CellInt(dgv.SelectedRows[0], "RegistrationID");

            DataTable dt = DBHelper.ExecuteQuery($@"
                SELECT e.Title, s.Name AS Society, e.Description,
                       CONVERT(varchar, e.EventDate, 106) AS Date, e.Location
                FROM EventRegistrations r
                JOIN Events    e ON r.EventID    = e.EventID
                JOIN Societies s ON e.SocietyID  = s.SocietyID
                WHERE r.RegistrationID = {regId}");

            if (dt.Rows.Count == 0) return;
            DataRow row = dt.Rows[0];

            // ── Ticket dialog ──────────────────────────────────────────────
            Form ticketForm = new Form
            {
                Text            = "Event Ticket Pass",
                Size            = new Size(500, 460),
                BackColor       = BgDark,
                ForeColor       = TextPrimary,
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
            };

            Panel accentBar = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = AccentOrange };
            ticketForm.Controls.Add(accentBar);

            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Padding = new Padding(30, 24, 30, 24) };
            ticketForm.Controls.Add(body);

            int ty = 0;
            var lTicket = new Label { Text = "🎟️  EVENT TICKET PASS", Font = FontH2, ForeColor = AccentOrange, AutoSize = true, Location = new Point(0, ty) }; ty += 38;
            var lId     = new Label { Text = $"Ticket #: TKT-{regId:0000000}", Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(0, ty) }; ty += 28;

            Panel div1 = new Panel { BackColor = Border, Height = 1, Width = 420, Location = new Point(0, ty) }; ty += 16;

            body.Controls.AddRange(new Control[] { lTicket, lId, div1 });
            TicketLine(body, "Event",   row["Title"].ToString()!,   AccentBlue,   ref ty);
            TicketLine(body, "Society", row["Society"].ToString()!, TextPrimary,  ref ty);
            TicketLine(body, "Date",    row["Date"].ToString()!,    AccentGreen,  ref ty);
            TicketLine(body, "Venue",   row["Location"].ToString()!, AccentPurple, ref ty);

            Panel div2 = new Panel { BackColor = Border, Height = 1, Width = 420, Location = new Point(0, ty) }; ty += 16;
            body.Controls.Add(div2);

            var lDesc = new Label
            {
                Text      = row["Description"].ToString(),
                Font      = FontSmall,
                ForeColor = TextMuted,
                Location  = new Point(0, ty),
                Size      = new Size(420, 60),
                AutoSize  = false,
            };
            ty += 68;

            var lGen = new Label { Text = $"Generated: {DateTime.Now:dd MMM yyyy  HH:mm}", Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(0, ty) };
            body.Controls.AddRange(new Control[] { lDesc, lGen });

            Button btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(110, 36),
                Location  = new Point(0, ty + 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, AccentRed.R, AccentRed.G, AccentRed.B),
                ForeColor = AccentRed,
                Font      = FontH3,
                Cursor    = Cursors.Hand,
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(60, AccentRed.R, AccentRed.G, AccentRed.B);
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.Click += (s, e) => ticketForm.Close();
            body.Controls.Add(btnClose);

            ticketForm.ShowDialog(this);
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    private void TicketLine(Panel body, string label, string value, Color accent, ref int y)
    {
        var lbl = new Label { Text = label + ":", Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(0,  y) };
        var val = new Label { Text = value,       Font = FontH3,    ForeColor = accent,    AutoSize = true, Location = new Point(90, y) };
        body.Controls.AddRange(new Control[] { lbl, val });
        y += 26;
    }

    private void DownloadTicket(DataGridView dgv)
    {
        if (!HasRow(dgv, "Select a ticket to download.")) return;
        try
        {
            int regId = CellInt(dgv.SelectedRows[0], "RegistrationID");

            using var sfd = new SaveFileDialog
            {
                Filter   = "Text File|*.txt",
                FileName = $"Ticket_TKT-{regId:0000000}_{DateTime.Now:yyyyMMdd}.txt",
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            DataTable dt = DBHelper.ExecuteQuery($@"
                SELECT e.Title, s.Name AS Society, e.Description,
                       CONVERT(varchar, e.EventDate, 106) AS Date, e.Location
                FROM EventRegistrations r
                JOIN Events    e ON r.EventID    = e.EventID
                JOIN Societies s ON e.SocietyID  = s.SocietyID
                WHERE r.RegistrationID = {regId}");

            if (dt.Rows.Count == 0) { Info("Ticket data not found."); return; }
            DataRow row = dt.Rows[0];

            string content =
                "╔══════════════════════════════════════════════╗\n"  +
                "║         FAST SOCIETIES MANAGEMENT SYSTEM     ║\n"  +
                "║              EVENT TICKET PASS               ║\n"  +
                "╠══════════════════════════════════════════════╣\n"  +
               $"║  Ticket ID   : TKT-{regId:0000000}                    \n"  +
                "╠══════════════════════════════════════════════╣\n"  +
               $"║  Event       : {row["Title"]}\n"                        +
               $"║  Society     : {row["Society"]}\n"                      +
               $"║  Date        : {row["Date"]}\n"                         +
               $"║  Venue       : {row["Location"]}\n"                     +
                "╠══════════════════════════════════════════════╣\n"  +
                "║  Description :\n"                                        +
               $"║  {row["Description"]}\n"                                 +
                "╠══════════════════════════════════════════════╣\n"  +
               $"║  Student ID  : {userId}\n"                              +
               $"║  Generated   : {DateTime.Now:dd MMM yyyy  HH:mm:ss}\n" +
                "╚══════════════════════════════════════════════╝\n\n" +
                "Please present this ticket at the event entrance.";

            File.WriteAllText(sfd.FileName, content);
            Toast("✅ Ticket downloaded successfully.");
        }
        catch (Exception ex) { Err(ex.Message); }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Data helpers
    // ══════════════════════════════════════════════════════════════════════
    private void LoadStudentName()
    {
        try
        {
            object? res = DBHelper.ExecuteScalar(
                "SELECT FirstName + ' ' + LastName FROM Users WHERE UserID=@U",
                new[] { P("@U", userId) });
            if (res != null && res != DBNull.Value) studentName = res.ToString()!;
        }
        catch { }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Clock / status bar
    // ══════════════════════════════════════════════════════════════════════
    private void StartClock()
    {
        // FIX: UpdateStatusBar already called in BuildUI(); timer keeps it fresh
        clockTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        clockTimer.Tick += (s, e) => UpdateStatusBar();
        clockTimer.Start();
    }

    private void UpdateStatusBar()
    {
        if (statusBarLabel == null) return;
        statusBarLabel.Text =
            $"  👤  {studentName}   •   🕐  {DateTime.Now:ddd, dd MMM yyyy  •  HH:mm}" +
            "   •   Societies Management System";
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Reusable UI builders
    // ══════════════════════════════════════════════════════════════════════
    private Panel SectionHeader(string title, string subtitle, Color accent, ref int y)
    {
        Panel p = new Panel
        {
            BackColor = Color.Transparent,
            Height    = 80,
            Location  = new Point(0, y),
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        y += 88;
        Panel bar = new Panel { BackColor = accent, Size = new Size(4, 46), Location = new Point(0, 8) };
        Label lbl = new Label { Text = title,    Font = FontTitle, ForeColor = TextPrimary, AutoSize = true, Location = new Point(18, 4) };
        Label sub = new Label { Text = subtitle, Font = FontBody,  ForeColor = TextMuted,   AutoSize = true, Location = new Point(18, 46) };
        p.Controls.AddRange(new Control[] { bar, lbl, sub });
        return p;
    }

    private Panel StatRow((string label, string value, Color accent)[] items)
    {
        int gap   = 14;
        int cardH = 92;
        // Cards will be sized dynamically once the row is added to its parent.
        // Use a fixed size now; resize hook applied after layout.
        int cardW = items.Length <= 3 ? 200 : 170;

        Panel row = new Panel
        {
            BackColor = Color.Transparent,
            Height    = cardH,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        Panel[] cards = new Panel[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            var (label, value, accent) = items[i];
            int ci = i;   // capture for lambda
            Panel card = new Panel
            {
                Size      = new Size(cardW, cardH),
                BackColor = BgCard,
                Location  = new Point(i * (cardW + gap), 0),
            };
            card.Paint += (s, e) => DrawBorder(e.Graphics, card.ClientRectangle,
                                               Color.FromArgb(50, items[ci].accent.R, items[ci].accent.G, items[ci].accent.B));

            Panel stripe = new Panel { BackColor = accent, Size = new Size(cardW, 3), Location = new Point(0, 0) };
            Label val    = new Label { Text = value, Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = accent, AutoSize = true, Location = new Point(14, 10) };
            Label lbl    = new Label { Text = label, Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(14, 60) };
            card.Controls.AddRange(new Control[] { stripe, val, lbl });
            row.Controls.Add(card);
            cards[i] = card;
        }

        // Reflow cards when row is resized (form resize propagates here)
        row.Resize += (s, e) =>
        {
            int usable  = row.Width;
            int dynCardW = (usable - gap * (items.Length - 1)) / items.Length;
            dynCardW = Math.Max(dynCardW, 130);
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Width    = dynCardW;
                cards[i].Location = new Point(i * (dynCardW + gap), 0);
                // Keep stripe width in sync
                if (cards[i].Controls.Count > 0 && cards[i].Controls[0] is Panel stripe2)
                    stripe2.Width = dynCardW;
            }
        };

        return row;
    }

    private Panel BuildToolbar(out TextBox searchBox, out Panel searchWrap, ref int y)
    {
        Panel toolbar = new Panel
        {
            BackColor = Color.Transparent,
            Height    = 52,
            Location  = new Point(0, y),
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        y += 58;

        searchWrap = new Panel
        {
            Size      = new Size(280, 38),
            BackColor = BgCard,
            Location  = new Point(0, 6),
        };
        // FIX CS1628: capture local so lambda does not close over out parameter
        Panel swLocal = searchWrap;
        swLocal.Paint += (s, e) => DrawBorder(e.Graphics, swLocal.ClientRectangle, Border);

        Label icon = new Label
        {
            Text      = "🔍",
            AutoSize  = true,
            Location  = new Point(8, 8),
            Font      = FontSmall,
            ForeColor = TextMuted,
        };
        searchBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor   = BgCard,
            ForeColor   = TextPrimary,
            Font        = FontBody,
            Location    = new Point(30, 9),
            Size        = new Size(240, 22),
        };
        searchWrap.Controls.AddRange(new Control[] { icon, searchBox });
        toolbar.Controls.Add(searchWrap);
        return toolbar;
    }

    private Button ToolBtn(string text, Color color)
    {
        Button btn = new Button
        {
            Text      = text,
            Height    = 38,
            AutoSize  = false,
            Width     = TextRenderer.MeasureText(text, FontH3).Width + 28,
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

    private DataGridView StyledGrid()
    {
        DataGridView dgv = new DataGridView
        {
            BackgroundColor               = BgCard,
            GridColor                     = Border,
            BorderStyle                   = BorderStyle.None,
            CellBorderStyle               = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode                 = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect                   = false,
            ReadOnly                      = true,
            AllowUserToAddRows            = false,
            AllowUserToDeleteRows         = false,
            AllowUserToResizeRows         = false,
            RowHeadersVisible             = false,
            AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.Fill,
            Font                          = FontBody,
            ForeColor                     = TextPrimary,
            ScrollBars                    = ScrollBars.Vertical,
            Anchor                        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnHeadersHeightSizeMode   = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight           = 42,
        };
        // Enable double-buffering via reflection to eliminate flicker
        typeof(DataGridView)
            .GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(dgv, true);

        dgv.DefaultCellStyle.BackColor          = BgCard;
        dgv.DefaultCellStyle.ForeColor          = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, AccentBlue.R, AccentBlue.G, AccentBlue.B);
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.DefaultCellStyle.Padding            = new Padding(4, 0, 4, 0);
        dgv.AlternatingRowsDefaultCellStyle.BackColor       = BgHover;
        dgv.ColumnHeadersDefaultCellStyle.BackColor         = Color.FromArgb(17, 22, 30);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor         = TextMuted;
        dgv.ColumnHeadersDefaultCellStyle.Font              = new Font("Segoe UI Semibold", 9f);
        dgv.ColumnHeadersDefaultCellStyle.Padding           = new Padding(8, 0, 0, 0);
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowTemplate.Height        = 38;
        return dgv;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Utility helpers
    // ══════════════════════════════════════════════════════════════════════
    private void SetActiveNav(int idx)
    {
        for (int i = 0; i < navButtons.Length; i++)
        {
            Color accent = sections[i].accent;
            bool  active = i == idx;
            navButtons[i].BackColor = active ? Color.FromArgb(22, accent.R, accent.G, accent.B) : Color.Transparent;
            navButtons[i].ForeColor = active ? accent : TextMuted;
        }
    }

    private void HideIdColumns(DataGridView dgv)
    {
        foreach (DataGridViewColumn col in dgv.Columns)
            if (col.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase)) col.Visible = false;
    }

    private void FilterGrid(DataGridView dgv, string text, string[] columns)
    {
        foreach (DataGridViewRow row in dgv.Rows)
        {
            bool show = string.IsNullOrWhiteSpace(text) ||
                columns.Any(c => dgv.Columns.Contains(c) &&
                            row.Cells[c].Value?.ToString()
                                ?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
            row.Visible = show;
        }
    }

    private void ColorizeColumn(DataGridView dgv, string colName, Func<string, Color> colorPicker)
    {
        dgv.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != colName) return;
            e.CellStyle.ForeColor = colorPicker(e.Value?.ToString() ?? "");
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

    private int SafeCount(string sql)
    {
        try { return Convert.ToInt32(DBHelper.ExecuteScalar(sql)); }
        catch { return 0; }
    }

    private bool HasRow(DataGridView dgv, string msg)
    {
        if (dgv.SelectedRows.Count > 0) return true;
        Info(msg); return false;
    }

    private void Toast(string msg) =>
        MessageBox.Show(msg, "Student Portal", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void Info(string msg) =>
        MessageBox.Show(msg, "Student Portal", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void Err(string msg) =>
        MessageBox.Show($"Error: {msg}", "Student Portal", MessageBoxButtons.OK, MessageBoxIcon.Error);

    // ── Null-safe cell accessors (eliminates CS8605 / CS8600) ─────────────
    /// <summary>Safely unboxes an integer cell value; returns 0 if null.</summary>
    private static int CellInt(DataGridViewRow row, string col) =>
        row.Cells[col].Value is { } v ? Convert.ToInt32(v) : 0;

    /// <summary>Returns a cell's string value or "" if null.</summary>
    private static string CellStr(DataGridViewRow row, string col) =>
        row.Cells[col].Value?.ToString() ?? "";
}