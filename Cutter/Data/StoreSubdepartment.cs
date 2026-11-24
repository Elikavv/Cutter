using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cutter.Data
{
    [Table("StoreSubdepartments")]
    public class StoreSubdepartment
    {
        [Key]
        public int Id { get; set; }

        public int StoreDepartmentId { get; set; }
        public virtual StoreDepartment Department { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Навигация
        public virtual ICollection<StoreItem> StoreItems { get; set; } = new List<StoreItem>();
    }
}