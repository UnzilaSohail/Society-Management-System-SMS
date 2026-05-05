using System.Windows.Forms;

namespace SocietiesManagementSystem;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Initialize database
        try
        {
            DBHelper.InitializeDatabase();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Database initialization failed: " + ex.Message);
            return;
        }

        Application.Run(new LoginForm());
    }    
}