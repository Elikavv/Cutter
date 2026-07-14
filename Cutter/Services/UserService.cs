using Cutter.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cutter.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<string?> GetUserRoleAsync(string userId)
        {
            return await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Role)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsInRoleAsync(string userId, string role)
        {
            var userRole = await GetUserRoleAsync(userId);
            return string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Store?> GetUserStoreAsync(string userId)
        {
            return await _db.Users
                .Where(u => u.Id == userId)
                .Include(u => u.Store)
                .Select(u => u.Store)
                .FirstOrDefaultAsync();
        }
    }
}
