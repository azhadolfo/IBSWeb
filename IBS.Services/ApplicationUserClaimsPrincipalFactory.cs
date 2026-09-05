using System.Security.Claims;
using IBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IBS.Services
{
    public class ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
    {
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            foreach (var claim in identity.FindAll(ClaimTypes.GivenName).ToList())
            {
                identity.RemoveClaim(claim);
            }

            var fullName = string.IsNullOrWhiteSpace(user.Name) ? user.UserName : user.Name;
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, fullName));
            }

            return identity;
        }
    }
}
