using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Cutter.Data
{

    public class CuttingSource
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Sheet Sheet { get; set; }
        public List<Detail> Details { get; set; }
        public bool IsActive { get; set; }
        public int QuantitySheet { get; set; } = 1;
        public int CutCount { get; set; } = 0;
        public int DetailsCount { get; set; } = 0;
    }

    public class CuttingPlan
    {
        public List<SheetLayout> SheetLayouts { get; set; } = new(); // Все листы с раскроем
        public List<Detail> UnplacedDetails { get; set; } = new(); // Неразмещенные детали
        public double TotalMaterialUsage { get; set; } // Общее использование материала
        public double TotalWastePercentage { get; set; } // Общий процент отходов
        public string? OptimizationAlgorithm { get; set; }
        public int TotalCuts { get; set; } // Общее количество резов
        public string? NumCut { get; set; }
        public bool IsSave { get; set; }
        public string? Invoice { get; set; }
    }

    public class SheetLayout
    {
        public Sheet Sheet { get; set; } // Сам лист
        public List<CutDetail> Details { get; set; } = new(); // Детали на этом листе
        public double MaterialUsage { get; set; } // Использование материала для этого листа
        public double WastePercentage { get; set; } // Отходы для этого листа
        public bool IsEmpty => Details.Count == 0; // Пустой ли лист
        public bool IsActive { get; set; }
        public List<CutLine> CutLines { get; set; } = new(); // Линии реза
        public int CutCount { get; set; } // Количество резов на листе
        public int DetailsCount { get; set; } // Количество таромест
        public List<CutFragment> CutFragments { get; set; } = new List<CutFragment>();
    }
    public class CutLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public string Type { get; set; } // "horizontal" или "vertical"
        public bool IsVisible { get; set; } = true;
        public int Stage { get; set; } = 1; // Этап распила
        public bool IsCut { get; set; } = false; // Выполнен ли рез

    }

    public class CutFragment
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public List<CutDetail> Details { get; set; } = new List<CutDetail>();
        public bool IsCut { get; set; } = false;
    }

    public class GuillotineCut
    {
        public CutLine CutLine { get; set; }
        public bool IsVertical { get; set; }
        public double Position { get; set; } // координата реза в локальной системе фрагмента
    }
    
    public class UserCuttingPlan
    {
        public required string UserName { get; set; }
        public required string StoreName { get; set; }
        public string? StoreAddress { get; set; }
        public string? Phone { get; set; }
        public string? CutBarCode { get; set; }
        public string? NumCut { get; set; }
        public List<CuttingPlan>? CuttingPlan { get; set; }
        public string? Invoice { get; set; }
    }
}
