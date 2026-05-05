using System.Drawing;
using System.Windows.Forms;

namespace SocietiesManagementSystem
{
    public static class PromptHelper
    {
        public static string? ShowInput(string title, string label)
        {
            using Form form = new Form();
            form.Text = title;
            form.Size = new Size(400, 170);
            form.StartPosition = FormStartPosition.CenterParent;
            Label labelControl = new Label() { Text = label, Left = 10, Top = 20, Width = 360 };
            TextBox textBox = new TextBox() { Left = 10, Top = 50, Width = 360 };
            Button ok = new Button() { Text = "OK", Left = 200, Width = 80, Top = 85, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 290, Width = 80, Top = 85, DialogResult = DialogResult.Cancel };
            form.Controls.AddRange(new Control[] { labelControl, textBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        public static string? ShowChoice(string title, string label, string[] options)
        {
            if (options == null)
                options = new string[0];

            using Form form = new Form();
            form.Text = title;
            form.Size = new Size(400, 180);
            form.StartPosition = FormStartPosition.CenterParent;
            Label labelControl = new Label() { Text = label, Left = 10, Top = 20, Width = 360 };
            ComboBox comboBox = new ComboBox() { Left = 10, Top = 50, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            comboBox.Items.AddRange(options);
            if (options.Length > 0) comboBox.SelectedIndex = 0;
            Button ok = new Button() { Text = "OK", Left = 200, Width = 80, Top = 90, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 290, Width = 80, Top = 90, DialogResult = DialogResult.Cancel };
            form.Controls.AddRange(new Control[] { labelControl, comboBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            return form.ShowDialog() == DialogResult.OK ? comboBox.SelectedItem?.ToString() : null;
        }
    }
}
