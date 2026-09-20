namespace BauManagement.Models
{
    public class WorkTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Explicit tenant key for safe, efficient company-scoped task queries.
        public Guid CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        public Guid BaustelleId { get; set; }

        public Baustelle Baustelle { get; set; } = null!;

        public Guid? EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Open;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
