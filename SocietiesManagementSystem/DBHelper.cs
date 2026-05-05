using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace SocietiesManagementSystem
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DBHelper — static database access layer
    //  FIX: was a non-static class whose methods were called statically
    //       everywhere; nested private methods inside InitializeDatabase()
    //       are invalid C# — extracted them to proper class-level members.
    // ═══════════════════════════════════════════════════════════════════════
    public static class DBHelper
    {
        // ── Connection string candidates ──────────────────────────────────
        private static readonly string[] CandidateConnectionStrings =
        {
            "Server=UNZILA-09\\Unzila Anjum;Database=SocietiesManagementDB;Trusted_Connection=True;TrustServerCertificate=True;",
            "Server=UNZILA-09\\SQLEXPRESS;Database=SocietiesManagementDB;Trusted_Connection=True;TrustServerCertificate=True;",
            "Server=localhost;Database=SocietiesManagementDB;Trusted_Connection=True;TrustServerCertificate=True;",
            "Server=localhost\\SQLEXPRESS;Database=SocietiesManagementDB;Trusted_Connection=True;TrustServerCertificate=True;",
            "Server=.\\SQLEXPRESS;Database=SocietiesManagementDB;Trusted_Connection=True;TrustServerCertificate=True;",
            "Server=.;Database=SocietiesManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
        };

        private static string _connectionString = string.Empty;

        // ── Public entry point called from Program.Main() ─────────────────
        public static void InitializeDatabase()
        {
            _connectionString = ResolveConnectionString();
            string masterConn = BuildMasterConnectionString(_connectionString);

            // Create database if it doesn't exist
            using (SqlConnection conn = new SqlConnection(masterConn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SocietiesManagementDB') " +
                    "CREATE DATABASE SocietiesManagementDB", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // Create all tables
            ExecuteNonQuery(@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
CREATE TABLE Users (
    UserID      INT IDENTITY(1,1) PRIMARY KEY,
    FirstName   NVARCHAR(50)  NOT NULL,
    LastName    NVARCHAR(50)  NOT NULL,
    Email       NVARCHAR(100) UNIQUE NOT NULL,
    Password    NVARCHAR(255) NOT NULL,
    Role        NVARCHAR(20)  NOT NULL CHECK (Role IN ('Student','SocietyHead','Admin')),
    CreatedDate DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Societies')
CREATE TABLE Societies (
    SocietyID   INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    HeadID      INT,
    Status      NVARCHAR(20) DEFAULT 'Active'
                    CHECK (Status IN ('Active','Suspended','Deleted')),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (HeadID) REFERENCES Users(UserID)
);

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Memberships')
CREATE TABLE Memberships (
    MembershipID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID    INT NOT NULL,
    SocietyID    INT NOT NULL,
    Status       NVARCHAR(20) DEFAULT 'Pending'
                     CHECK (Status IN ('Pending','Approved','Rejected')),
    JoinDate     DATETIME DEFAULT GETDATE(),
    ApprovedDate DATETIME NULL,
    FOREIGN KEY (StudentID)  REFERENCES Users(UserID),
    FOREIGN KEY (SocietyID)  REFERENCES Societies(SocietyID)
);

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Events')
CREATE TABLE Events (
    EventID     INT IDENTITY(1,1) PRIMARY KEY,
    SocietyID   INT NOT NULL,
    Title       NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    EventDate   DATETIME NOT NULL,
    Location    NVARCHAR(200),
    Status      NVARCHAR(20) DEFAULT 'Pending'
                    CHECK (Status IN ('Pending','Approved','Cancelled')),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SocietyID) REFERENCES Societies(SocietyID)
);

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EventRegistrations')
CREATE TABLE EventRegistrations (
    RegistrationID   INT IDENTITY(1,1) PRIMARY KEY,
    StudentID        INT NOT NULL,
    EventID          INT NOT NULL,
    RegistrationDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (StudentID) REFERENCES Users(UserID),
    FOREIGN KEY (EventID)   REFERENCES Events(EventID)
);

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Tasks')
CREATE TABLE Tasks (
    TaskID      INT IDENTITY(1,1) PRIMARY KEY,
    SocietyID   INT NOT NULL,
    AssignedTo  INT NOT NULL,
    Title       NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Status      NVARCHAR(20) DEFAULT 'Pending'
                    CHECK (Status IN ('Pending','InProgress','Completed')),
    DueDate     DATETIME,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SocietyID)  REFERENCES Societies(SocietyID),
    FOREIGN KEY (AssignedTo) REFERENCES Users(UserID)
);");

            // Create indexes
            ExecuteNonQuery(@"
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
    CREATE INDEX IX_Users_Email ON Users(Email);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Memberships_StudentID')
    CREATE INDEX IX_Memberships_StudentID ON Memberships(StudentID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Memberships_SocietyID')
    CREATE INDEX IX_Memberships_SocietyID ON Memberships(SocietyID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_SocietyID')
    CREATE INDEX IX_Events_SocietyID ON Events(SocietyID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EventRegistrations_StudentID')
    CREATE INDEX IX_EventRegistrations_StudentID ON EventRegistrations(StudentID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EventRegistrations_EventID')
    CREATE INDEX IX_EventRegistrations_EventID ON EventRegistrations(EventID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tasks_SocietyID')
    CREATE INDEX IX_Tasks_SocietyID ON Tasks(SocietyID);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tasks_AssignedTo')
    CREATE INDEX IX_Tasks_AssignedTo ON Tasks(AssignedTo);");

            // Seed default admin account
            if (Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Role='Admin'")) == 0)
            {
                string hashedAdmin = HashPassword("admin123");
                ExecuteNonQuery(
                    "INSERT INTO Users (FirstName,LastName,Email,Password,Role) " +
                    "VALUES ('Admin','User','admin@university.edu',@P,'Admin')",
                    new[] { new SqlParameter("@P", hashedAdmin) });
            }

            // Seed sample data
            SeedSampleData();
        }

        // ── Seed sample data ──────────────────────────────────────────────
        private static void SeedSampleData()
        {
            string samplePassword = HashPassword("password123");

            ExecuteNonQuery(@"
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'student1@university.edu')
BEGIN
    INSERT INTO Users (FirstName,LastName,Email,Password,Role) VALUES ('Student','One','student1@university.edu',@SP,'Student');
    INSERT INTO Users (FirstName,LastName,Email,Password,Role) VALUES ('Student','Two','student2@university.edu',@SP,'Student');
    INSERT INTO Users (FirstName,LastName,Email,Password,Role) VALUES ('Alice','Head','head1@university.edu',@SP,'SocietyHead');
    INSERT INTO Users (FirstName,LastName,Email,Password,Role) VALUES ('Bob','Head','head2@university.edu',@SP,'SocietyHead');
END

IF NOT EXISTS (SELECT 1 FROM Societies WHERE Name = 'Gaming Club')
BEGIN
    DECLARE @h1 INT = (SELECT UserID FROM Users WHERE Email='head1@university.edu');
    IF @h1 IS NOT NULL
        INSERT INTO Societies (Name,Description,HeadID,Status)
        VALUES ('Gaming Club','A club for competitive and casual gamers.',@h1,'Active');
END

IF NOT EXISTS (SELECT 1 FROM Societies WHERE Name = 'Sports Society')
BEGIN
    DECLARE @h2 INT = (SELECT UserID FROM Users WHERE Email='head2@university.edu');
    IF @h2 IS NOT NULL
        INSERT INTO Societies (Name,Description,HeadID,Status)
        VALUES ('Sports Society','Organizes intramural sports, training, and student competitions.',@h2,'Active');
END

IF NOT EXISTS (SELECT 1 FROM Societies WHERE Name = 'Creative Arts')
BEGIN
    DECLARE @h3 INT = (SELECT UserID FROM Users WHERE Email='head2@university.edu');
    IF @h3 IS NOT NULL
        INSERT INTO Societies (Name,Description,HeadID,Status)
        VALUES ('Creative Arts','Art, drama, and music activities for creative students.',@h3,'Active');
END

IF NOT EXISTS (SELECT 1 FROM Memberships
    WHERE StudentID=(SELECT UserID FROM Users WHERE Email='student1@university.edu')
      AND SocietyID=(SELECT SocietyID FROM Societies WHERE Name='Gaming Club'))
BEGIN
    INSERT INTO Memberships (StudentID,SocietyID,Status,ApprovedDate)
    VALUES (
        (SELECT UserID  FROM Users    WHERE Email='student1@university.edu'),
        (SELECT SocietyID FROM Societies WHERE Name='Gaming Club'),
        'Approved', GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM Memberships
    WHERE StudentID=(SELECT UserID FROM Users WHERE Email='student2@university.edu')
      AND SocietyID=(SELECT SocietyID FROM Societies WHERE Name='Sports Society'))
BEGIN
    INSERT INTO Memberships (StudentID,SocietyID,Status,ApprovedDate)
    VALUES (
        (SELECT UserID  FROM Users    WHERE Email='student2@university.edu'),
        (SELECT SocietyID FROM Societies WHERE Name='Sports Society'),
        'Approved', GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM Events WHERE Title='Campus LAN Night')
BEGIN
    INSERT INTO Events (SocietyID,Title,Description,EventDate,Location,Status) VALUES
        ((SELECT SocietyID FROM Societies WHERE Name='Gaming Club'),
         'Campus LAN Night','Join us for evening tournaments and friendly gaming.',
         DATEADD(day,10,GETDATE()),'Room 201','Approved');
END

IF NOT EXISTS (SELECT 1 FROM Events WHERE Title='Spring Sports Fest')
BEGIN
    INSERT INTO Events (SocietyID,Title,Description,EventDate,Location,Status) VALUES
        ((SELECT SocietyID FROM Societies WHERE Name='Sports Society'),
         'Spring Sports Fest','A weekend of matches, skills training, and team bonding.',
         DATEADD(day,18,GETDATE()),'Outdoor Courts','Approved');
END

IF NOT EXISTS (SELECT 1 FROM Events WHERE Title='Creative Showcase')
BEGIN
    INSERT INTO Events (SocietyID,Title,Description,EventDate,Location,Status) VALUES
        ((SELECT SocietyID FROM Societies WHERE Name='Creative Arts'),
         'Creative Showcase','Showcase student art, music, and performances.',
         DATEADD(day,24,GETDATE()),'Auditorium','Pending');
END

IF NOT EXISTS (SELECT 1 FROM EventRegistrations
    WHERE StudentID=(SELECT UserID FROM Users WHERE Email='student1@university.edu')
      AND EventID  =(SELECT EventID FROM Events WHERE Title='Campus LAN Night'))
BEGIN
    INSERT INTO EventRegistrations (StudentID,EventID)
    VALUES (
        (SELECT UserID  FROM Users  WHERE Email='student1@university.edu'),
        (SELECT EventID FROM Events WHERE Title='Campus LAN Night'));
END

IF NOT EXISTS (SELECT 1 FROM EventRegistrations
    WHERE StudentID=(SELECT UserID FROM Users WHERE Email='student2@university.edu')
      AND EventID  =(SELECT EventID FROM Events WHERE Title='Spring Sports Fest'))
BEGIN
    INSERT INTO EventRegistrations (StudentID,EventID)
    VALUES (
        (SELECT UserID  FROM Users  WHERE Email='student2@university.edu'),
        (SELECT EventID FROM Events WHERE Title='Spring Sports Fest'));
END

IF NOT EXISTS (SELECT 1 FROM Tasks WHERE Title='Prepare tournament brackets')
BEGIN
    INSERT INTO Tasks (SocietyID,AssignedTo,Title,Description,Status,DueDate) VALUES
        ((SELECT SocietyID FROM Societies WHERE Name='Gaming Club'),
         (SELECT UserID FROM Users WHERE Email='head1@university.edu'),
         'Prepare tournament brackets',
         'Create match schedules and player groups for the upcoming LAN night.',
         'InProgress', DATEADD(day,7,GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM Tasks WHERE Title='Book sports equipment')
BEGIN
    INSERT INTO Tasks (SocietyID,AssignedTo,Title,Description,Status,DueDate) VALUES
        ((SELECT SocietyID FROM Societies WHERE Name='Sports Society'),
         (SELECT UserID FROM Users WHERE Email='head2@university.edu'),
         'Book sports equipment',
         'Reserve equipment and refreshment setup for the Spring Sports Fest.',
         'Pending', DATEADD(day,9,GETDATE()));
END",
                new[] { new SqlParameter("@SP", samplePassword) });
        }

        // ── Connection resolution ─────────────────────────────────────────
        private static string ResolveConnectionString()
        {
            string? env = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            var failures = new List<string>();

            if (!string.IsNullOrWhiteSpace(env))
            {
                if (TryOpenMaster(env, out string? envError))
                    return env;
                failures.Add($"DB_CONNECTION_STRING → {envError}");
            }

            foreach (string candidate in CandidateConnectionStrings)
            {
                if (TryOpenMaster(candidate, out string? err))
                    return candidate;
                failures.Add($"{candidate} → {err}");
            }

            throw new InvalidOperationException(
                "Unable to connect to SQL Server. Ensure SQL Server is running.\n\n" +
                "Tried:\n" + string.Join("\n", failures) + "\n\n" +
                "Set the DB_CONNECTION_STRING environment variable to override.");
        }

        private static bool TryOpenMaster(string cs, out string? errorMessage)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(BuildMasterConnectionString(cs));
                conn.Open();
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static string BuildMasterConnectionString(string cs)
        {
            var b = new SqlConnectionStringBuilder(cs) { InitialCatalog = "master" };
            return b.ConnectionString;
        }

        // ── Password hashing ──────────────────────────────────────────────
        private static string HashPassword(string password)
        {
            using System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Public data-access methods (static — matches call sites)
        // ═════════════════════════════════════════════════════════════════
        public static SqlConnection GetConnection()
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("DBHelper.InitializeDatabase() must be called first.");
            return new SqlConnection(_connectionString);
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            using SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            object? result = cmd.ExecuteScalar();
            return (result == null || result == DBNull.Value) ? null : result;
        }
    }
}