namespace SocietiesManagementSystem;

// ═══════════════════════════════════════════════════════════════════════════
//  StudentDashboard.Designer.cs
//  FIX: stub had AutoScaleMode=Font, ClientSize=800×600, Text="StudentDashboard".
//       Corrected to Dpi scaling and 1320×860 matching BuildUI().
// ═══════════════════════════════════════════════════════════════════════════
partial class StudentDashboard
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        this.SuspendLayout();

        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Dpi;
        this.ClientSize          = new System.Drawing.Size(1320, 860);
        this.MinimumSize         = new System.Drawing.Size(1100, 700);
        this.Text                = "Student Portal — Societies Management System";
        this.Name                = "StudentDashboard";
        this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.Sizable;
        this.BackColor           = System.Drawing.Color.FromArgb(13, 17, 23);
        this.ForeColor           = System.Drawing.Color.FromArgb(230, 237, 243);
        this.DoubleBuffered      = true;
        this.Font                = new System.Drawing.Font("Segoe UI", 9.5f);

        this.ResumeLayout(false);
    }
}