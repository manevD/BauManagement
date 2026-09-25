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

    public async Task<bool> CanUseApplicationAsync(Guid companyId)
    {
        var company = await GetCompanyAsync(companyId);

        if (company is null)
            return false;

        if (company.SubscriptionStatus == SubscriptionStatus.Cancelled)
            return false;

        if (company.SubscriptionStatus == SubscriptionStatus.Expired)
            return false;

        if (company.SubscriptionStatus == SubscriptionStatus.PastDue)
            return false;

        if (company.SubscriptionStatus == SubscriptionStatus.Trial)
        {
            return company.TrialEndDate > DateTime.UtcNow;
        }

        if (company.SubscriptionStatus == SubscriptionStatus.Active)
        {
            return company.IsSubscriptionActive;
        }

        return false;
    }

    public async Task<bool> NeedsNewSubscriptionAsync(Guid companyId)
    {
        var company = await GetCompanyAsync(companyId);

        if (company is null)
            return true;

        return company.SubscriptionStatus == SubscriptionStatus.Cancelled
            || company.SubscriptionStatus == SubscriptionStatus.Expired;
    }
}