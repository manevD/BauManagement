namespace BauManagement.Models
{

    public class Company
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.S;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime TrialStartDate { get; set; }

        public DateTime TrialEndDate { get; set; }

        public bool IsTrialActive =>
            DateTime.UtcNow >= TrialStartDate &&
            DateTime.UtcNow <= TrialEndDate;

        public bool IsSubscriptionActive { get; set; }

        public string? StripeCustomerId { get; set; }

        public string? StripeSubscriptionId { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public ICollection<Employee> Employees { get; set; }
            = new List<Employee>();

        public ICollection<Baustelle> Baustellen { get; set; }
            = new List<Baustelle>();
    }
    public enum SubscriptionStatus
    {
        Trial = 0,
        Active = 1,
        Expired = 2,
        Cancelled = 3,
        PastDue = 4
    }
}
