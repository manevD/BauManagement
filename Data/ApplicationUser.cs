using Microsoft.AspNetCore.Identity;
using BauManagement.Models;

namespace BauManagement.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public Guid? CompanyId { get; set; }

        public Company? Company { get; set; }
    }

}
