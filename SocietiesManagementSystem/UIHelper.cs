using System.Drawing;
using System.Windows.Forms;

namespace SocietiesManagementSystem
{
    public static class UIHelper
    {
        public static readonly Color BackgroundColor = Color.FromArgb(20, 28, 44);
        public static readonly Color PanelColor = Color.FromArgb(26, 34, 50);
        public static readonly Color AccentColor = Color.FromArgb(52, 152, 219);
        public static readonly Color TextColor = Color.FromArgb(235, 240, 250);
        public static readonly Font HeaderFont = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font ButtonFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);

        public static void ApplyAppTheme(Form form)
        {
            form.BackColor = BackgroundColor;
            form.Font = BodyFont;
            form.ForeColor = TextColor;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Padding = new Padding(12);
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.MinimizeBox = true;
        }

        public static Panel CreateHeaderPanel(string title, string subtitle = null)
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = PanelColor
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = HeaderFont,
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            header.Controls.Add(lblTitle);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                Label lblSubtitle = new Label
                {
                    Text = subtitle,
                    Font = TitleFont,
                    ForeColor = Color.FromArgb(180, 210, 220),
                    AutoSize = true,
                    Location = new Point(20, 52)
                };
                header.Controls.Add(lblSubtitle);
            }

            return header;
        }

        public static void StyleButton(Button button)
        {
            button.BackColor = AccentColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = ButtonFont;
            button.Height = 36;
            button.Margin = new Padding(6);
        }

        public static void StyleLabel(Label label, bool isHeader = false)
        {
            label.ForeColor = TextColor;
            label.Font = isHeader ? TitleFont : BodyFont;
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = Color.FromArgb(50, 58, 76);
            textBox.ForeColor = TextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = BodyFont;
        }

        public static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.BackColor = Color.FromArgb(50, 58, 76);
            comboBox.ForeColor = TextColor;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.Font = BodyFont;
        }

        public static void StyleDateTimePicker(DateTimePicker picker)
        {
            picker.CalendarForeColor = TextColor;
            picker.CalendarMonthBackground = Color.FromArgb(50, 58, 76);
            picker.CalendarTitleBackColor = PanelColor;
            picker.CalendarTitleForeColor = TextColor;
            picker.CalendarTrailingForeColor = Color.FromArgb(180, 210, 220);
            picker.Font = BodyFont;
            picker.Width = 240;
        }

        public static void StyleDataGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = PanelColor;
            dgv.ForeColor = TextColor;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(34, 40, 56);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            dgv.ColumnHeadersDefaultCellStyle.Font = TitleFont;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(46, 54, 72);
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = AccentColor;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(40, 48, 66);
            dgv.RowHeadersVisible = false;
            dgv.GridColor = Color.FromArgb(60, 70, 90);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.RowTemplate.Height = 34;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ScrollBars = ScrollBars.Both;
        }

        public static void StyleTabControl(TabControl tabControl)
        {
            tabControl.Font = TitleFont;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.ItemSize = new Size(120, 36);
            tabControl.Padding = new Point(12, 7);
            tabControl.DrawItem += (sender, args) =>
            {
                TabPage page = tabControl.TabPages[args.Index];
                Rectangle bounds = args.Bounds;
                Color backColor = args.State == DrawItemState.Selected ? AccentColor : PanelColor;
                using Brush brush = new SolidBrush(backColor);
                args.Graphics.FillRectangle(brush, bounds);
                TextRenderer.DrawText(args.Graphics, page.Text, TitleFont, bounds, TextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        public static void StyleTabPage(TabPage page)
        {
            page.BackColor = BackgroundColor;
            page.ForeColor = TextColor;
            page.Padding = new Padding(12);
            page.AutoScroll = true;
        }
    }
}
