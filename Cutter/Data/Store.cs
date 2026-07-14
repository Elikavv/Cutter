namespace Cutter.Data
{
    public class Store
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CutBarCode { get; set; } = string.Empty;
        public string StoreCode { get; set; } = string.Empty;

        // Статус магазина
        public bool IsActive { get; set; } = true;

        // Подписка
        public DateTime? SubscriptionExpiresAt { get; set; }

        // Вычисляемое свойство (на сервере!)
        public bool IsSubscriptionActive => SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt > DateTime.Now;

        public ICollection<StoreItem> StoreItems { get; set; } = new List<StoreItem>();

        public ICollection<StoreMaterialCutPrice> StoreMaterialCutPrices { get; set; } = new List<StoreMaterialCutPrice>();
    }
}
