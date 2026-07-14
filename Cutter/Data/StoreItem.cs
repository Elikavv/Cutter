using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    public class StoreItem
    {
        public Guid StoreId { get; set; }
        public Store Store { get; set; } = null!;

        public int ItemId { get; set; }
        public Items Item { get; set; } = null!;

        public float Price { get; set; }
        public bool IsActive { get; set; } = true;
        public int? MinOrderQuantity { get; set; }
    }
}
