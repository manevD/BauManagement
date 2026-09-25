using BauManagement.Data;
using BauManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Services;

public class SubscriptionService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Company?> GetCompanyAsync(Guid companyId)
    {
        return await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == companyId);
    }

    public async Task<int> GetEmployeeCountAsync(Guid companyId)
    {
        return await _db.Employees
            .CountAsync(x =>
                x.CompanyId == companyId &&
                x.IsActive);
    }

    public async Task<SubscriptionPlan> GetMinimumPlanAsync(Guid companyId)
    {
        var employeeCount = await GetEmployeeCountAsync(companyId);

        return employeeCount switch
        {
            <= 5 => SubscriptionPlan.S,
            <= 10 => SubscriptionPlan.M,
            <= 20 => SubscriptionPlan.L,
            _ => SubscriptionPlan.XL
        };
    }

    public async Task<bool> IsPlanAllowedAsync(
        Guid companyId,
        SubscriptionPlan selectedPlan)
    {
        var minimumPlan = await GetMinimumPlanAsync(companyId);

        return selectedPlan >= minimumPlan;
    }

    /// <summary>
    /// Zentrale Prüfung, ob die Company die Anwendung verwenden darf.
    /// Diese Methode ist die einzige authoritative Subscription-Prüfung.
    /// </summary>
    public async Task<SubscriptionAccessResult> CanUseApplicationAsync(
        Guid companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == companyId);

        if (company is null)
        {
            return SubscriptionAccessResult.Blocked(
                SubscriptionBlockReason.CompanyNotFound);
        }

        switch (company.SubscriptionStatus)
        {
            case SubscriptionStatus.Trial:

                if (company.TrialEndDate <= DateTime.UtcNow)
                {
                    return SubscriptionAccessResult.Blocked(
                        SubscriptionBlockReason.TrialExpired);
                }

                return SubscriptionAccessResult.Allowed();

            case SubscriptionStatus.Active:

                if (!company.IsSubscriptionActive)
                {
                    return SubscriptionAccessResult.Blocked(
                        SubscriptionBlockReason.SubscriptionInactive);
                }

                if (company.CurrentPeriodEnd.HasValue &&
                    company.CurrentPeriodEnd.Value <= DateTime.UtcNow)
                {
                    return SubscriptionAccessResult.Blocked(
                        SubscriptionBlockReason.SubscriptionExpired);
                }

                return SubscriptionAccessResult.Allowed();

            case SubscriptionStatus.Cancelled:

                return SubscriptionAccessResult.Blocked(
                    SubscriptionBlockReason.Cancelled);

            case SubscriptionStatus.Expired:

                return SubscriptionAccessResult.Blocked(
                    SubscriptionBlockReason.Expired);

            case SubscriptionStatus.PastDue:

                return SubscriptionAccessResult.Blocked(
                    SubscriptionBlockReason.PastDue);

            default:

                return SubscriptionAccessResult.Blocked(
                    SubscriptionBlockReason.Unknown);
        }
    }

    public async Task<bool> NeedsNewSubscriptionAsync(Guid companyId)
    {
        var result = await CanUseApplicationAsync(companyId);

        return result.Reason == SubscriptionBlockReason.Cancelled
            || result.Reason == SubscriptionBlockReason.Expired
            || result.Reason == SubscriptionBlockReason.TrialExpired;
    }
}


public enum SubscriptionBlockReason
{
    None = 0,

    CompanyNotFound = 1,

    TrialExpired = 2,

    SubscriptionInactive = 3,

    SubscriptionExpired = 4,

    Cancelled = 5,

    Expired = 6,

    PastDue = 7,

    Unknown = 8
}


public sealed class SubscriptionAccessResult
{
    public bool IsAllowed { get; private set; }

    public SubscriptionBlockReason Reason { get; private set; }

    private SubscriptionAccessResult()
    {
    }

    public static SubscriptionAccessResult Allowed()
    {
        return new SubscriptionAccessResult
        {
            IsAllowed = true,
            Reason = SubscriptionBlockReason.None
        };
    }

    public static SubscriptionAccessResult Blocked(
        SubscriptionBlockReason reason)
    {
        return new SubscriptionAccessResult
        {
            IsAllowed = false,
            Reason = reason
        };
    }
}