namespace BauManagement.Models
{
    public class Employee
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? ApplicationUserId { get; set; }

        public Data.ApplicationUser? ApplicationUser { get; set; }

        public string? Position { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WorkAssignment> WorkAssignments { get; set; }
            = new List<WorkAssignment>();

        public ICollection<WorkTask> Tasks { get; set; }
            = new List<WorkTask>();
    }
}
