
namespace BauManagement.Models
{
    public class CompanyAvv
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CompanyId { get; set; }

        public string Version { get; set; } = string.Empty;

        public DateTime AcceptedAtUtc { get; set; }

        public string AcceptedByUserId { get; set; } = string.Empty;

        public string? DocumentHash { get; set; }

        public string? DocumentPath { get; set; }

        public Company? Company { get; set; }
    }
}
