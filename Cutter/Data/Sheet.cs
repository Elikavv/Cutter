using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    public class Sheet
    {
        public string Uid { get; set; } = Guid.NewGuid().ToString();
        public int Id { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }

        public double Depth { get; set; }
        public string? Name { get; set; }
        public required string SKU { get; set; }
        public required string BarCode { get; set; }
        public double Price { get; set; }
        public double CutPrice { get; set; }
        public int Quantity { get; set; } = 1;

        // Технологические ограничения
        public double BladeWidth { get; set; } = 3.2; // Ширина реза
        public double MinMargin { get; set; } = 0; // Минимальный отступ от края
        public double MinDistanceBetweenParts { get; set; } = 0; // Минимальное расстояние между деталями
        public bool CanRotateParts { get; set; } = true; // Разрешен поворот деталей
        public double MaxCutLength { get; set; } = 3000; // Максимальная длина реза

        public double MarginTop { get; set; } = 0;      // Отступ сверху (по оси Y)
        public double MarginBottom { get; set; } = 0;   // Отступ снизу (по оси Y)
        public double MarginLeft { get; set; } = 0;     // Отступ слева (по оси X)
        public double MarginRight { get; set; } = 0;    // Отступ справа (по оси X)

        public int? MaterialTypeId { get; set; }
        public MaterialType? MaterialType { get; set; }

        public Sheet Clone()
        {
            return new Sheet
            {
                Uid = Guid.NewGuid().ToString(),
                Id = this.Id,
                Length = this.Length,
                Width = this.Width,
                Depth = this.Depth,
                Name = this.Name,
                SKU = this.SKU,
                BarCode = this.BarCode,
                Price = this.Price,
                Quantity = 1,
                BladeWidth = this.BladeWidth,
                MinMargin = this.MinMargin,
                MinDistanceBetweenParts = this.MinDistanceBetweenParts,
                CanRotateParts = this.CanRotateParts,
                MaxCutLength = this.MaxCutLength,
                CutPrice = this.CutPrice,
                MarginTop = this.MarginTop,
                MarginBottom = this.MarginBottom,
                MarginLeft = this.MarginLeft,
                MarginRight = this.MarginRight
            };
        }
    }


}
