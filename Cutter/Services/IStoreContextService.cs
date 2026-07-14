namespace Cutter.Services
{
    public interface IStoreContextService
    {
        Task<StoreStatus> GetStatusAsync(string userId);
        Task<bool> CanAccessFullFeaturesAsync(string userId);
    }

    public class StoreStatus
    {
        public bool IsStoreActive { get; set; }
        public bool IsSubscriptionActive { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public bool CanSignIn => IsStoreActive;
        public bool CanUseFullFeatures => IsStoreActive && IsSubscriptionActive;
    }
}
