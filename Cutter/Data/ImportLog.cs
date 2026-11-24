using Cutter.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ImportLogs")]
public class ImportLog
{
    [Key]
    public int Id { get; set; }

    public Guid StoreId { get; set; }
    public virtual Store Store { get; set; } = null!;

    // === КТО ЗАГРУЗИЛ ===
    public string? UserId { get; set; }  // null = автоматическая загрузка
    public virtual ApplicationUser? User { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "Manual";
    // Варианты: "Manual", "API", "Scheduled", "FileUpload"

    // === ЧТО ЗАГРУЖАЛИ ===
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }  // полезно для мониторинга

    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public int ProcessingTimeMs { get; set; }  // сколько длился импорт

    // === НАСТРОЙКИ ИМПОРТА (сохраняем JSON-ом) ===
    [Column(TypeName = "jsonb")]
    public string? SettingsJson { get; set; }
    // Например: {"CreateNewDepartments":true,"UpdatePrices":false,"OverwriteDuplicates":true}

    // === РЕЗУЛЬТАТЫ ===
    public int TotalRows { get; set; }
    public int CreatedItems { get; set; }
    public int UpdatedItems { get; set; }
    public int CreatedDepartments { get; set; }
    public int CreatedSubdepartments { get; set; }
    public int SkippedRows { get; set; }
    public int ErrorsCount { get; set; }

    [Column(TypeName = "text")]
    public string? ErrorMessage { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Completed";
    // Completed, Failed, Partial, Cancelled
}