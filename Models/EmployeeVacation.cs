using System.ComponentModel.DataAnnotations;

namespace BauManagement.Models;

public enum VacationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class EmployeeVacation
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? Notes { get; set; }

    public VacationStatus Status { get; set; } = VacationStatus.Pending;

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}