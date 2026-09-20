using BauManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Services;

public sealed class CompanyCancellationService(ApplicationDbContext db)
{
    public async Task CancelAsync(Guid companyId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var employeeIds = await db.Employees.Where(x => x.CompanyId == companyId).Select(x => x.Id).ToListAsync();
        var userIds = await db.Users.Where(x => x.CompanyId == companyId).Select(x => x.Id).ToListAsync();

        await db.WorkAssignments.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.WorkTasks.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Employees.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Baustellen.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Users.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        await db.Companies.Where(x => x.Id == companyId).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }
}
