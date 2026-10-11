
using BauManagement.Data;
using BauManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BauManagement.Services;

public sealed class CompanyDataCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<CompanyDataCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Провери еднаш при стартување, потоа на секои 24 часа.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeleteExpiredCompaniesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Company data cleanup failed.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DeleteExpiredCompaniesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<ApplicationDbContext>>();

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // За платена претплата, рокот од 3 месеци почнува
        // по крајот на платениот период.
        // За компании без CurrentPeriodEnd, користи го
        // CancelledAt како резервен датум.
        var companies = await db.Companies
            .AsNoTracking()
            .Where(c =>
                c.SubscriptionStatus == SubscriptionStatus.Cancelled &&
                !c.IsSubscriptionActive &&
                c.CancelledAt != null)
            .ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var endDate =
                company.CurrentPeriodEnd ?? company.CancelledAt!.Value;

            if (endDate.AddMonths(3) > now)
                continue;

            await using var companyDb =
                await factory.CreateDbContextAsync(cancellationToken);

            await using var transaction =
                await companyDb.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // Повторна проверка во базата пред бришење.
                var stillEligible = await companyDb.Companies
                    .AnyAsync(c =>
                        c.Id == company.Id &&
                        c.SubscriptionStatus == SubscriptionStatus.Cancelled &&
                        !c.IsSubscriptionActive &&
                        c.CancelledAt != null &&
                        (c.CurrentPeriodEnd ?? c.CancelledAt.Value)
                            <= now.AddMonths(-3),
                        cancellationToken);

                if (!stillEligible)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                // Земи ги корисниците поврзани со компанијата
                // или со нејзините вработени.
                var employeeUserIds = await companyDb.Employees
                    .Where(e =>
                        e.CompanyId == company.Id &&
                        e.ApplicationUserId != null)
                    .Select(e => e.ApplicationUserId!)
                    .ToListAsync(cancellationToken);

                // Избриши ги зависните записи пред родителските.
                await companyDb.EmployeeVacations
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.WorkTimeEntries
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.WorkAssignments
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.WorkTasks
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.CompanyAvvs
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.Employees
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.Baustellen
                    .Where(x => x.CompanyId == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                // Кориснички сметки поврзани со компанијата
                // или со нејзините вработени.
                await companyDb.Users
                    .Where(u =>
                        u.CompanyId == company.Id ||
                        employeeUserIds.Contains(u.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                await companyDb.Companies
                    .Where(c => c.Id == company.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Deleted company data for company {CompanyId}.",
                    company.Id);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
