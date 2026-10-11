
using BauManagement.Data;
using BauManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Services;

public sealed class CompanyCancellationService(
    IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task CancelAsync(Guid companyId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var company = await db.Companies
            .FirstOrDefaultAsync(x => x.Id == companyId);

        if (company is null)
            return;

        // Евидентирај го отказот, но не бриши податоци.
        company.CancelledAt ??= DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}
