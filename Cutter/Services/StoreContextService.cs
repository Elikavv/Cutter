namespace Cutter.Services
{
    public class StoreContextService : IStoreContextService
    {
        private readonly IUserService _userService;
        private readonly IStoreAccessService _storeAccess;
        private readonly ISubscriptionService _subscription;

        public StoreContextService(
            IUserService userService,
            IStoreAccessService storeAccess,
            ISubscriptionService subscription)
        {
            _userService = userService;
            _storeAccess = storeAccess;
            _subscription = subscription;
        }

        public async Task<StoreStatus> GetStatusAsync(string userId)
        {
            var store = await _userService.GetUserStoreAsync(userId);
            if (store == null)
            {
                return new StoreStatus
                {
                    IsStoreActive = false,
                    IsSubscriptionActive = false
                };
            }

            var isSubActive = await _subscription.IsSubscriptionActiveAsync(store.Id);

            return new StoreStatus
            {
                IsStoreActive = store.IsActive,
                IsSubscriptionActive = isSubActive,
                SubscriptionExpiresAt = await _subscription.GetSubscriptionExpiryAsync(store.Id)
            };
        }

        public async Task<bool> CanAccessFullFeaturesAsync(string userId)
        {
            var status = await GetStatusAsync(userId);
            return status.CanUseFullFeatures;
        }
    }
}
