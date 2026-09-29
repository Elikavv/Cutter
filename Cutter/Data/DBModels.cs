using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Cutter.Data
{
    public class DBModels
    {
        [Table("Items")]
        public class Items
        {
            [Key]
            public int Id { get; set; }
            [Required]
            public required string Name { get; set; }
            [Required]
            public required string BarCode { get; set; }
            [Required]
            public required string SKU { get; set; }
            [Required]
            public int Length { get; set; }
            [Required]
            public int Width { get; set; }
            [Required]
            public float Depth { get; set; }
            public string? Descr { get; set; }
            [Column(TypeName = "jsonb")]
            public string? Images { get; set; }
            public float Price { get; set; }
            public bool IsActive { get; set; } = true;
            public int MaterialTypeId { get; set; } 
            public MaterialType MaterialType { get; set; } = null!;
            public ICollection<StoreItem> StoreItems { get; set; } = new List<StoreItem>();

        }

        [Table("MaterialType")]
        public class MaterialType
        {
            public int Id { get; set; }
            public required string MaterialName { get; set; }
            public string? Descr { get; set; }
            // 🔗 Навигация: в каких листах используется
            public ICollection<Items> Items { get; set; } = new List<Items>();

            // 🔗 Навигация: какая цена реза в каких магазинах
            public ICollection<StoreMaterialCutPrice> StoreMaterialCutPrices { get; set; } = new List<StoreMaterialCutPrice>();

            // 🔗 НОВОЕ: Услуга по умолчанию для этого типа материала
            public int? DefaultCuttingServiceId { get; set; }
            public CuttingServiceModel? DefaultCuttingService { get; set; }
        }

        [Table("CuttingPlan")]
        public class CutPlan
        {
            [Key]
            public int Id { get; set; }
            [Required]
            public required string GUUID { get; set; }
            [Required]
            public string? UserId { get; set; }
            [Column("modifier_id")]
            public string? ModifierId { get; set; }
            [Column(TypeName = "jsonb")]
            public string? CuttingPlan { get; set; }
            [Column("create_date")]
            public DateTime CreateDate { get; set; } = DateTime.Now;
            [Column("modify_date")]
            public DateTime ModifyDate { get; set; } = DateTime.Now;
            [Column("is_save")]
            public bool IsSave { get; set; } = false;
            public virtual ApplicationUser User { get; set; }
            public virtual ApplicationUser Modifier { get; set; }
            public string? StaffId { get; set; }
            public string NumCut { get; set; } = string.Empty;
            public string? Invoice { get; set; }
            [Column("is_paid")]
            public bool IsPaid { get; set; } = true;
            [Column("save_date")]
            public DateTime? SaveDate { get; set; }
        }

    }
}
