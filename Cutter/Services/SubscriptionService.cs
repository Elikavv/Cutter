using Cutter.Data;
using Microsoft.EntityFrameworkCore;

namespace Cutter.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _db;

        public SubscriptionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsSubscriptionActiveAsync(Guid storeId)
        {
            return await _db.Stores
                .Where(s => s.Id == storeId)
                .Select(s => s.SubscriptionExpiresAt.HasValue && s.SubscriptionExpiresAt > DateTime.Now)
                .FirstOrDefaultAsync();
        }

        public async Task<DateTime?> GetSubscriptionExpiryAsync(Guid storeId)
        {
            return await _db.Stores
                .Where(s => s.Id == storeId)
                .Select(s => s.SubscriptionExpiresAt)
                .FirstOrDefaultAsync();
        }
    }
}
