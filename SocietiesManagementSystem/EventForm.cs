using Microsoft.Data.SqlClient;
using System.Data;

namespace SocietiesManagementSystem;

public partial class EventForm : Form
{
    private int societyId;
    private int eventId;

    private TextBox txtTitle;
    private TextBox txtDesc;
    private DateTimePicker dtpDate;
    private TextBox txtLocation;
    private Button btnSave;
    private Button btnCancel;

    // Colors
    private static readonly Color BgDark = Color.FromArgb(13, 17, 23);
    private static readonly Color BgCard = Color.FromArgb(22, 27, 34);
    private static readonly Color AccentBlue = Color.FromArgb(56, 139, 253);
    private static readonly Color AccentGreen = Color.FromArgb(63, 185, 80);
    private static readonly Color AccentRed = Color.FromArgb(248, 81, 73);
    private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
    private static readonly Color TextMuted = Color.FromArgb(125, 133, 144);
    private static readonly Color Border = Color.FromArgb(48, 54, 61);
    private static readonly Color InputBg = Color.FromArgb(13, 17, 23);

    private static readonly Font FontTitle = new Font("Segoe UI", 16f, FontStyle.Bold);
    private static readonly Font FontLabel = new Font("Segoe UI Semibold", 9.5f);
    private static readonly Font FontInput = new Font("Segoe UI", 10f);
    private static readonly Font FontSmall = new Font("Segoe UI", 8.5f);
    private static readonly Font FontButton = new Font("Segoe UI Semibold", 10f);

    public EventForm(int societyId, int eventId = 0)
    {
        this.societyId = societyId;
        this.eventId = eventId;
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        bool isEdit = eventId > 0;

        this.Text = isEdit ? "Edit Event" : "Create New Event";
        this.Size = new Size(560, 500); // smaller to allow scrolling
        this.MinimumSize = new Size(500, 450);
        this.BackColor = BgDark;
        this.ForeColor = TextPrimary;
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Top bar
        Panel accentBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 3,
            BackColor = isEdit ? AccentGreen : AccentBlue,
        };
        this.Controls.Add(accentBar);

        // Header
        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = BgCard,
        };

        Label lblTitle = new Label
        {
            Text = isEdit ? "Edit Event" : "Create Event",
            Font = FontTitle,
            ForeColor = isEdit ? AccentGreen : AccentBlue,
            AutoSize = true,
            Location = new Point(24, 14),
        };

        Label lblSub = new Label
        {
            Text = isEdit
                ? "Update event details — will require re-approval."
                : "Fill in the details to submit a new event request.",
            Font = FontSmall,
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(24, 46),
        };

        header.Controls.Add(lblTitle);
        header.Controls.Add(lblSub);
        this.Controls.Add(header);

        // Divider
        Panel divider = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Border
        };
        this.Controls.Add(divider);

        // Scrollable body
        Panel body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(28, 20, 28, 20),
            AutoScroll = true,
            AutoScrollMargin = new Size(0, 20),
        };
        this.Controls.Add(body);

        // Content wrapper (IMPORTANT)
        Panel content = new Panel
        {
            Width = 480,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent
        };
        body.Controls.Add(content);

        // Center content
        body.Resize += (s, e) =>
        {
            content.Left = (body.ClientSize.Width - content.Width) / 2;
        };

        int y = 0;

        // Title
        content.Controls.Add(MakeLabel("Event Title", new Point(0, y)));
        y += 24;

        txtTitle = MakeTextBox(new Point(0, y), 480);
        content.Controls.Add(txtTitle);
        y += txtTitle.Height + 18;

        // Description
        content.Controls.Add(MakeLabel("Description", new Point(0, y)));
        y += 24;

        txtDesc = MakeTextBox(new Point(0, y), 480, 110, true);
        content.Controls.Add(txtDesc);
        y += txtDesc.Height + 18;

        // Date
        content.Controls.Add(MakeLabel("Event Date", new Point(0, y)));
        y += 24;

        dtpDate = new DateTimePicker
        {
            Location = new Point(0, y),
            Size = new Size(480, 38),
            Font = FontInput,
            Format = DateTimePickerFormat.Long,
        };
        content.Controls.Add(dtpDate);
        y += dtpDate.Height + 18;

        // Location
        content.Controls.Add(MakeLabel("Venue / Location", new Point(0, y)));
        y += 24;
body.AutoScrollPosition = new Point(0, 0);
        txtLocation = MakeTextBox(new Point(0, y), 480);
        content.Controls.Add(txtLocation);
        y += txtLocation.Height + 28;

        // Buttons
        Panel btnRow = new Panel
        {
            Bounds = new Rectangle(0, y, 480, 48)
        };

        btnSave = new Button
        {
            Text = isEdit ? "Save Changes" : "Submit Event",
            Size = new Size(220, 44),
            Location = new Point(0, 0),
            BackColor = isEdit ? AccentGreen : AccentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(130, 44),
            Location = new Point(234, 0),
            BackColor = AccentRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };

        btnRow.Controls.Add(btnSave);
        btnRow.Controls.Add(btnCancel);
        content.Controls.Add(btnRow);

        // Events
        btnSave.Click += (s, e) => SaveEvent();
        btnCancel.Click += (s, e) => this.Close();

        if (isEdit) LoadEventDetails();
    }

    private void LoadEventDetails()
    {
        string query = "SELECT Title, Description, EventDate, Location FROM Events WHERE EventID = @EventID";
        SqlParameter[] p = { new("@EventID", eventId) };

        DataTable dt = DBHelper.ExecuteQuery(query, p);
        if (dt.Rows.Count > 0)
        {
            DataRow row = dt.Rows[0];
            txtTitle.Text = row["Title"].ToString();
            txtDesc.Text = row["Description"].ToString();
            dtpDate.Value = Convert.ToDateTime(row["EventDate"]);
            txtLocation.Text = row["Location"].ToString();
        }
    }

    private void SaveEvent()
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("Title required");
            return;
        }

        string query;

        if (eventId == 0)
        {
            query = @"INSERT INTO Events (SocietyID, Title, Description, EventDate, Location, Status)
                      VALUES (@SocietyID, @Title, @Description, @EventDate, @Location, 'Pending')";
        }
        else
        {
          // Change the update query to also reset Status to Pending:
query = @"UPDATE Events 
          SET Title=@Title, Description=@Description,
              EventDate=@EventDate, Location=@Location,
              Status='Pending'
          WHERE EventID=@EventID";
        }

        SqlParameter[] p = {
            new("@SocietyID", societyId),
            new("@Title", txtTitle.Text),
            new("@Description", txtDesc.Text),
            new("@EventDate", dtpDate.Value),
            new("@Location", txtLocation.Text),
            new("@EventID", eventId)
        };

        DBHelper.ExecuteNonQuery(query, p);

        MessageBox.Show("Saved successfully");
        this.Close();
    }

    private Label MakeLabel(string text, Point loc)
    {
        return new Label
        {
            Text = text,
            Location = loc,
            ForeColor = TextMuted,
            AutoSize = true
        };
    }

    private TextBox MakeTextBox(Point loc, int w, int h = 38, bool multiline = false)
    {
        return new TextBox
        {
            Location = loc,
            Size = new Size(w, h),
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };
    }
}