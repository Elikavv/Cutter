namespace Cutter.Services
{
    public interface ISubscriptionService
    {
        Task<bool> IsSubscriptionActiveAsync(Guid storeId);
        Task<DateTime?> GetSubscriptionExpiryAsync(Guid storeId);
    }
}
