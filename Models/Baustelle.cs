namespace BauManagement.Models
{
    public class Baustelle
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WorkAssignment> WorkAssignments { get; set; }
            = new List<WorkAssignment>();

        public ICollection<WorkTask> Tasks { get; set; }
            = new List<WorkTask>();
    }
}
