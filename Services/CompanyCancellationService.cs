using BauManagement.Data;
using BauManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Services;

public sealed class CompanyCancellationService(ApplicationDbContext db)
{
    public async Task CancelAsync(Guid companyId)
    {
        var company = await db.Companies
            .FirstOrDefaultAsync(x => x.Id == companyId);

        if (company is null)
            return;

        company.SubscriptionStatus = SubscriptionStatus.Cancelled;

        company.IsSubscriptionActive = false;

        company.CancelledAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
    //public async Task CancelAsync(Guid companyId)
    //{
    //    await using var transaction = await db.Database.BeginTransactionAsync();

    //    await db.EmployeeVacations.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.WorkTimeEntries.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.WorkAssignments.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.WorkTasks.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.Employees.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.Baustellen.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.Users.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
    //    await db.Companies.Where(x => x.Id == companyId).ExecuteDeleteAsync();

    //    await transaction.CommitAsync();
    //}
}
