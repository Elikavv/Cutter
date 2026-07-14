using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    public class StoreMaterialCutPrice
    {
        public Guid StoreId { get; set; }
        public Store Store { get; set; } = null!;

        public int MaterialTypeId { get; set; }
        public MaterialType MaterialType { get; set; } = null!;

        public float CutPrice { get; set; } 

        public bool IsActive { get; set; } = true;
    }
}
