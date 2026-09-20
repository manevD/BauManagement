using BauManagement.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BauManagement.Services;

public interface ICurrentCompanyContext
{
    Task<Guid?> GetCompanyIdAsync();
}

public sealed class CurrentCompanyContext(
    AuthenticationStateProvider authenticationStateProvider,
    UserManager<ApplicationUser> userManager) : ICurrentCompanyContext
{
    public async Task<Guid?> GetCompanyIdAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = await userManager.GetUserAsync(state.User);
        return user?.CompanyId;
    }
}
