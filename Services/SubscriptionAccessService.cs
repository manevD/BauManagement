using BauManagement.Data;
using BauManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Services;

public class SubscriptionAccessService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionAccessService(
        ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasAccessAsync(Guid companyId)
    {
        var company =
            await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == companyId);

        if (company is null)
            return false;

        // Active paid subscription
        if (company.IsSubscriptionActive &&
            company.SubscriptionStatus == SubscriptionStatus.Active)
        {
            return true;
        }

        // Active trial
        if (company.SubscriptionStatus == SubscriptionStatus.Trial &&
            company.TrialEndDate > DateTime.UtcNow)
        {
            return true;
        }

        return false;
    }
}