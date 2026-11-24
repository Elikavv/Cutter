using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cutter.Data
{
    [Table("StoreDepartments")]
    public class StoreDepartment
    {
        [Key]
        public int Id { get; set; }

        public Guid StoreId { get; set; }
        public virtual Store Store { get; set; } = null!;

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
        public virtual ICollection<StoreSubdepartment> Subdepartments { get; set; } = new List<StoreSubdepartment>();
    }
}