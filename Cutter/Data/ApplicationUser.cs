using Microsoft.AspNetCore.Identity;

namespace Cutter.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; } = string.Empty; 
        public Guid StoreId { get; set; }
        public string Role { get; set; } = "User";
        public virtual Store Store { get; set; } = null!;
    }

}
