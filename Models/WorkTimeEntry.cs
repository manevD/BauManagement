namespace BauManagement.Models
{
    public class WorkTimeEntry
    {
        public Guid Id { get; set; }

        // Multi-Tenant
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // Mitarbeiter
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        // Baustelle
        public Guid BaustelleId { get; set; }
        public Baustelle Baustelle { get; set; } = null!;

        // Datum
        public DateTime WorkDate { get; set; }

        // Arbeitszeit
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // Pause später
        public int BreakMinutes { get; set; }

        // Status
        public WorkTimeStatus Status { get; set; }

        // Falls Mitarbeiter vergessen hat zu stoppen
        public bool IsManuallyCorrected { get; set; }

        // Wer hat korrigiert?
        public Guid? CorrectedByUserId { get; set; }

        // Notiz bei Korrektur
        public string? CorrectionNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public enum WorkTimeStatus
    {
        NotStarted = 0,
        Running = 1,
        Paused = 2,
        Completed = 3,
        MissingEnd = 4,
        Corrected = 5
    }
}
