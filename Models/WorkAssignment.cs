namespace BauManagement.Models
{
    public class WorkAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Kept explicitly on the aggregate so tenant-scoped queries never need
        // to infer the company through Employee or Baustelle.
        public Guid CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        public Guid EmployeeId { get; set; }

        public Employee Employee { get; set; } = null!;

        public Guid BaustelleId { get; set; }

        public Baustelle Baustelle { get; set; } = null!;

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
