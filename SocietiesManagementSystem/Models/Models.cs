namespace SocietiesManagementSystem.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class Society
    {
        public int SocietyID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? HeadID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class Membership
    {
        public int MembershipID { get; set; }
        public int StudentID { get; set; }
        public int SocietyID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }

    public class Event
    {
        public int EventID { get; set; }
        public int SocietyID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class EventRegistration
    {
        public int RegistrationID { get; set; }
        public int StudentID { get; set; }
        public int EventID { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class Task
    {
        public int TaskID { get; set; }
        public int SocietyID { get; set; }
        public int AssignedTo { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}