using Microsoft.Data.SqlClient;
using System.Data;

namespace SocietiesManagementSystem;

// ═══════════════════════════════════════════════════════════════════════════
//  TaskForm — Assign a new task to a society member  (FR 2.5)
//  FIX 1: INSERT query was missing Status column → violated CHECK constraint.
//  FIX 2: Form closed without setting DialogResult → grid never refreshed.
//  FIX 3: Styling now matches the dark GitHub-inspired palette used by
//          SocietyDashboard (UIHelper colours were a mismatch).
//  FIX 4: Empty-member guard prevents SelectedValue null-ref crash.
// ═══════════════════════════════════════════════════════════════════════════
public partial class TaskForm : Form
{
    private readonly int societyId;

    // ── Design tokens (identical to SocietyDashboard) ─────────────────────
    private static readonly Color BgDark      = Color.FromArgb(13,  17,  23);
    private static readonly Color BgCard      = Color.FromArgb(22,  27,  34);
    private static readonly Color AccentGreen = Color.FromArgb(63,  185, 80);
    private static readonly Color AccentRed   = Color.FromArgb(248, 81,  73);
    private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
    private static readonly Color TextMuted   = Color.FromArgb(125, 133, 144);
    private static readonly Color Border      = Color.FromArgb(48,  54,  61);
    private static readonly Color InputBg     = Color.FromArgb(13,  17,  23);

    private static readonly Font FontTitle  = new Font("Segoe UI", 16f, FontStyle.Bold);
    private static readonly Font FontLabel  = new Font("Segoe UI Semibold", 9.5f);
    private static readonly Font FontInput  = new Font("Segoe UI", 10f);
    private static readonly Font FontSmall  = new Font("Segoe UI", 8.5f);
    private static readonly Font FontButton = new Font("Segoe UI Semibold", 10f);

    public TaskForm(int societyId)
    {
        this.societyId = societyId;
        InitializeComponent();
        BuildUI();
    }

    // ── Designer stub ─────────────────────────────────────────────────────
    private System.ComponentModel.Container components = null!;
    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }
    private void InitializeComponent()
    {
        this.components    = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  UI construction
    // ═══════════════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        this.Text            = "Assign Task";
        this.Size            = new Size(580, 620);
        this.MinimumSize     = new Size(540, 580);
        this.BackColor       = BgDark;
        this.ForeColor       = TextPrimary;
        this.StartPosition   = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox     = false;
        this.DoubleBuffered  = true;

        // Accent bar
        this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 4, BackColor = AccentGreen });

        // Header
        Panel header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = BgCard };
        header.Controls.Add(new Label
        {
            Text = "Assign Task", Font = FontTitle, ForeColor = AccentGreen,
            AutoSize = true, Location = new Point(24, 14)
        });
        header.Controls.Add(new Label
        {
            Text = "Assign a task to an approved member of your society.",
            Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(24, 46)
        });
     
        // Body
        Panel body = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = BgDark,
            Padding    = new Padding(28, 18, 28, 18),
            AutoScroll = true,
        };
        this.Controls.Add(body);
   // Divider
        this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Border });
this.Controls.Add(header);

        // Inner panel holds all controls at fixed layout so AutoScroll works
        Panel inner = new Panel
        {
            BackColor  = BgDark,
            AutoScroll = false,
            Location   = new Point(0, 0),
            Padding = new Padding(28, 18, 28, 18),
            Width      = 490,
            Height     = 600,  // tall enough for all controls; trimmed after layout
        };
        body.Controls.Add(inner);

        int y = 0;

        // Title
        inner.Controls.Add(MakeLabel("Task Title *", new Point(0, y)));        y += 24;
        TextBox txtTitle = MakeTextBox(new Point(0, y), 490);
        inner.Controls.Add(txtTitle);                                           y += txtTitle.Height + 16;

        // Description
        inner.Controls.Add(MakeLabel("Description", new Point(0, y)));          y += 24;
        TextBox txtDesc = MakeTextBox(new Point(0, y), 490, 100, multiline: true);
        inner.Controls.Add(txtDesc);                                            y += txtDesc.Height + 16;

        // Assign To
        inner.Controls.Add(MakeLabel("Assign To (Approved Member) *", new Point(0, y))); y += 24;
        ComboBox cmbAssignee = new ComboBox
        {
            Location      = new Point(0, y),
            Size          = new Size(490, 38),
            Font          = FontInput,
            BackColor     = InputBg,
            ForeColor     = TextPrimary,
            FlatStyle     = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        inner.Controls.Add(cmbAssignee);                                        y += cmbAssignee.Height + 16;

        // Due Date
        inner.Controls.Add(MakeLabel("Due Date *", new Point(0, y)));           y += 24;
        DateTimePicker dtpDue = new DateTimePicker
        {
            Location = new Point(0, y),
            Size     = new Size(490, 38),
            Font     = FontInput,
            Format   = DateTimePickerFormat.Long,
            MinDate  = DateTime.Today.AddDays(1),
            Value    = DateTime.Today.AddDays(7),
        };
        inner.Controls.Add(dtpDue);                                             y += dtpDue.Height + 26;

        // Buttons
        Button btnSave = MakeButton("  ✔  Assign Task", AccentGreen, new Point(0, y), 200);
        Button btnCancel = new Button
        {
            Text      = "  ✖  Cancel",
            Size      = new Size(130, 44),
            Location  = new Point(212, y),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, AccentRed.R, AccentRed.G, AccentRed.B),
            ForeColor = AccentRed,
            Font      = FontButton,
            Cursor    = Cursors.Hand,
        };
        btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(60, AccentRed.R, AccentRed.G, AccentRed.B);
        btnCancel.FlatAppearance.BorderSize         = 1;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, AccentRed.R, AccentRed.G, AccentRed.B);
        inner.Controls.AddRange(new Control[] { btnSave, btnCancel });
        y += btnSave.Height + 16;
        inner.Height = y + 20;   // trim to actual content

        // Keep inner width in sync with body (accounts for scrollbar)
        body.Resize += (s, e) => inner.Width = Math.Max(body.ClientSize.Width - body.Padding.Left - body.Padding.Right, 200);

        // ── Load members after controls are created ────────────────────────
        LoadMembers(cmbAssignee);

        // ── Wire events ───────────────────────────────────────────────────
        btnSave.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                Shake(txtTitle);
                MessageBox.Show("Task title is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbAssignee.SelectedItem is not MemberItem selected)
            {
                MessageBox.Show("Please select a member to assign the task to.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dtpDue.Value.Date <= DateTime.Today)
            {
                MessageBox.Show("Due date must be in the future.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            AssignTask(txtTitle.Text.Trim(), txtDesc.Text.Trim(), selected.UserId, dtpDue.Value);
        };
        btnCancel.Click += (s, e) => this.Close();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Data
    // ═══════════════════════════════════════════════════════════════════════
    private void LoadMembers(ComboBox cmb)
    {
        try
        {
            DataTable dt = DBHelper.ExecuteQuery(
                @"SELECT u.UserID, u.FirstName + ' ' + u.LastName AS FullName
                  FROM Memberships m
                  JOIN Users u ON m.StudentID = u.UserID
                  WHERE m.SocietyID = @SID AND m.Status = 'Approved'
                  ORDER BY u.FirstName",
                new[] { new SqlParameter("@SID", societyId) });

            cmb.Items.Clear();
            if (dt.Rows.Count == 0)
            {
                cmb.Items.Add(new MemberItem("— No approved members yet —", 0));
                cmb.SelectedIndex = 0;
                // Disable save if no members exist
                foreach (Control c in this.Controls)
                    if (c is Panel body)
                        foreach (Control btn in body.Controls)
                            if (btn is Button b && b.Text.Contains("Assign Task"))
                                b.Enabled = false;
                return;
            }

            foreach (DataRow row in dt.Rows)
                cmb.Items.Add(new MemberItem(row["FullName"].ToString()!, (int)row["UserID"]));

            cmb.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load members: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AssignTask(string title, string desc, int assignedTo, DateTime dueDate)
    {
        try
        {
            // FIX: Include Status='Pending' to satisfy the CHECK constraint.
            DBHelper.ExecuteNonQuery(
                @"INSERT INTO Tasks (SocietyID, AssignedTo, Title, Description, Status, DueDate)
                  VALUES (@SID, @AssignedTo, @Title, @Desc, 'Pending', @DueDate)",
                new SqlParameter[]
                {
                    new("@SID",        societyId),
                    new("@AssignedTo", assignedTo),
                    new("@Title",      title),
                    new("@Desc",       desc),
                    new("@DueDate",    dueDate),
                });

            MessageBox.Show("Task assigned successfully.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // FIX: Set DialogResult so FormClosed handler in SocietyDashboard
            //      knows the grid needs refreshing.
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to assign task: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════════
    private Label MakeLabel(string text, Point loc) =>
        new Label { Text = text, Font = FontLabel, ForeColor = TextMuted, AutoSize = true, Location = loc };

    private TextBox MakeTextBox(Point loc, int w, int h = 38, bool multiline = false)
    {
        var tb = new TextBox
        {
            Location    = loc,
            Size        = new Size(w, h),
            Font        = FontInput,
            BackColor   = InputBg,
            ForeColor   = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline   = multiline,
            ScrollBars  = multiline ? ScrollBars.Vertical : ScrollBars.None,
        };
        tb.Enter += (s, e) => tb.BackColor = BgCard;
        tb.Leave += (s, e) => tb.BackColor = InputBg;
        return tb;
    }

    private Button MakeButton(string text, Color color, Point loc, int width)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(width, 44),
            Location  = loc,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font      = FontButton,
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize         = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Max(0, color.R - 22),
            Math.Max(0, color.G - 22),
            Math.Max(0, color.B - 22));
        return btn;
    }

    private static void Shake(Control ctrl)
    {
        int origX = ctrl.Left;
        for (int i = 0; i < 6; i++)
        {
            ctrl.Left = origX + (i % 2 == 0 ? 4 : -4);
            ctrl.Update();
            System.Threading.Thread.Sleep(30);
        }
        ctrl.Left = origX;
    }

    // ── ComboBox item wrapper ─────────────────────────────────────────────
    private sealed class MemberItem
    {
        public string Text   { get; }
        public int    UserId { get; }
        public MemberItem(string text, int userId) { Text = text; UserId = userId; }
        public override string ToString() => Text;
    }
}