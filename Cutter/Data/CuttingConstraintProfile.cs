using System.ComponentModel.DataAnnotations;

namespace Cutter.Data
{
    public class CuttingConstraintProfile
    {
        public int Id { get; set; }

        [Required]
        public Guid StoreId { get; set; } // Привязка к магазину

        [Required, MaxLength(100)]
        public string Name { get; set; } = "Стандартный профиль";

        public bool IsDefault { get; set; } // Флаг профиля по умолчанию для этого магазина

        // Технологические параметры
        public double BladeWidth { get; set; } = 3.2; // Ширина реза (мм)

        public double MarginTop { get; set; } = 0;    // Отступ сверху (мм)
        public double MarginBottom { get; set; } = 0; // Отступ снизу (мм)
        public double MarginLeft { get; set; } = 0;   // Отступ слева (мм)
        public double MarginRight { get; set; } = 0;  // Отступ справа (мм)

        public double MinDistanceBetweenParts { get; set; } = 0; // Мин. расстояние между деталями
        public bool CanRotateParts { get; set; } = true;         // Разрешен поворот
        public double MaxCutLength { get; set; } = 3000;         // Макс. длина реза

        // Связь с магазином
        public virtual Store Store { get; set; } = null!;
    }
}