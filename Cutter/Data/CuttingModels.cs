using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    // Услуги резки (справочник)
    [Table("CuttingServices")]
    public class CuttingServiceModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        public string? Description { get; set; }

        public bool IsExtraService { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Column("modify_date")]
        public DateTime ModifyDate { get; set; } = DateTime.Now;

        // 🔗 Обратная навигация: к каким типам материалов эта услуга привязана как основная
        public ICollection<MaterialType> MaterialTypes { get; set; } = new List<MaterialType>();

    }

    // Цены услуг по магазинам
    [Table("StoreServicePrices")]
    public class StoreServicePrice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }
        public Store Store { get; set; } = null!;

        public int ServiceId { get; set; }
        public CuttingServiceModel Service { get; set; } = null!;

        public float Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}