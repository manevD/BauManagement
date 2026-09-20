using BauManagement.Models;

namespace BauManagement.Services
{
    public class PlanningService
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Employee> Employees { get; set; }
            = new List<Employee>();

        public ICollection<Baustelle> Baustellen { get; set; }
            = new List<Baustelle>();
    }
}
