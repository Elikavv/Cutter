// Services/ApplicationUserClaimsPrincipalFactory.cs
using Cutter.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Cutter.Services
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Добавляем роль из свойства ApplicationUser.Role
            if (!string.IsNullOrWhiteSpace(user.Role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
            }

            return identity;
        }
    }
}