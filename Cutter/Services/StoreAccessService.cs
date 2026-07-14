using Cutter.Data;
using Microsoft.EntityFrameworkCore;

namespace Cutter.Services
{
    public class StoreAccessService : IStoreAccessService
    {
        private readonly ApplicationDbContext _db;

        public StoreAccessService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CanUserSignInAsync(string userId)
        {
            var isActive = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Store.IsActive)
                .FirstOrDefaultAsync();

            return isActive;
        }
    }
}
