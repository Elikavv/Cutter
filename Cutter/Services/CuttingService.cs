using Cutter.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace Cutter.Services
{
    /*********Rectangle class**************/
    public class Rectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Area => Width * Height;

        public Rectangle(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool IntersectsWith(Rectangle other, double tolerance = 0.0)
        {
            // Вычисляем координаты границ текущего прямоугольника с учетом допуска
            double left1 = X - tolerance;
            double right1 = X + Width + tolerance;
            double top1 = Y - tolerance;
            double bottom1 = Y + Height + tolerance;

            // Вычисляем координаты границ другого прямоугольника с учетом допуска
            double left2 = other.X - tolerance;
            double right2 = other.X + other.Width + tolerance;
            double top2 = other.Y - tolerance;
            double bottom2 = other.Y + other.Height + tolerance;

            // Проверяем пересечение по осям X и Y
            bool intersectsX = left1 < right2 && right1 > left2;
            bool intersectsY = top1 < bottom2 && bottom1 > top2;

            // Прямоугольники пересекаются, если пересекаются по обеим осям
            return intersectsX && intersectsY;
        }

        // Проверяет, содержится ли другой прямоугольник внутри этого
        public bool Contains(Rectangle other)
        {
            return X <= other.X &&
                   Y <= other.Y &&
                   X + Width >= other.X + other.Width &&
                   Y + Height >= other.Y + other.Height;
        }

        // Проверяет, можно ли объединить с другим прямоугольником
        public bool CanMergeWith(Rectangle other)
        {
            // Объединяем по горизонтали (рядом)
            if (Math.Abs(Y - other.Y) < 0.1 &&
                Math.Abs(Height - other.Height) < 0.1 &&
                (Math.Abs(X + Width - other.X) < 0.1 ||
                 Math.Abs(other.X + other.Width - X) < 0.1))
            {
                return true;
            }

            // Объединяем по вертикали (один под другим)
            if (Math.Abs(X - other.X) < 0.1 &&
                Math.Abs(Width - other.Width) < 0.1 &&
                (Math.Abs(Y + Height - other.Y) < 0.1 ||
                 Math.Abs(other.Y + other.Height - Y) < 0.1))
            {
                return true;
            }

            return false;
        }

        // Объединяет два прямоугольника
        public Rectangle Merge(Rectangle other)
        {
            double newX = Math.Min(X, other.X);
            double newY = Math.Min(Y, other.Y);
            double newWidth = Math.Max(X + Width, other.X + other.Width) - newX;
            double newHeight = Math.Max(Y + Height, other.Y + other.Height) - newY;

            return new Rectangle(newX, newY, newWidth, newHeight);
        }

    }

    public class PlacementOption
    {
        public double Width { get; set; }
        public double Length { get; set; }
        public double WidthWithBlade { get; set; }
        public double LengthWithBlade { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool Rotated { get; set; }
        public double Waste { get; set; }
        public Rectangle Rect { get; set; }
    }


    /**************************************/
    public class CuttingService
    {

        private readonly ApplicationDbContext _db;
        //private readonly DBContext _dbData;

        public CuttingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public CuttingPlan OptimizeCutting(List<Sheet> availableSheets, List<Detail> details)
        {
            return GuillotineAlgorithm3.GuillotineCut(availableSheets, details);
        }

        public async Task<string?> StoreCuttingPlan(List<CuttingPlan> plans, string UserId = null, string? Invoice = null)
        {
            if(plans == null || plans.Count == 0 || string.IsNullOrEmpty(UserId)) return null;

            var guid = Guid.NewGuid().ToString();

            var NumCut = _db.Users.Where(x => x.Id == UserId).Select(s => s.Store.StoreCode).FirstOrDefault() + DateTime.Now.ToString("ddMMyyHHmm");
            foreach (var item in plans)
            {
                item.NumCut = NumCut;
            }

            _db.CutPlan.Add(new DBModels.CutPlan
            {
                GUUID = guid,
                NumCut = NumCut,
                CuttingPlan = JsonConvert.SerializeObject(plans),
                UserId = UserId ?? null,
                Invoice = string.IsNullOrEmpty(Invoice) ? null : Invoice
            });

            await _db.SaveChangesAsync();

            return guid;

        }

        public async Task<UserCuttingPlan> GetCuttingPlanUser(string HashId)
        {
            if (string.IsNullOrEmpty(HashId)) return null;

            return  await _db.CutPlan
                    .Where(x => x.GUUID == HashId)
                    .Select(cp => new UserCuttingPlan
                    { 
                        StoreName = cp.User.Store.Name,
                        UserName = cp.User.UserName,
                        Phone = cp.User.Store.Phone,
                        StoreAddress = cp.User.Store.Address,
                        CutBarCode = cp.User.Store.CutBarCode,
                        NumCut = cp.NumCut,
                        CuttingPlan = JsonConvert.DeserializeObject<List<CuttingPlan>>(cp.CuttingPlan),
                        Invoice = cp.Invoice
                    }
                    ).FirstOrDefaultAsync();

            //return JsonConvert.DeserializeObject<List<CuttingPlan>>(_db.CutPlan.FirstOrDefault(p => p.GUUID == HashId).CuttingPlan);

        }

        public async Task<List<CuttingPlan>> GetCuttingPlan(string HashId)
        {
            if (string.IsNullOrEmpty(HashId)) return null;

            return JsonConvert.DeserializeObject<List<CuttingPlan>>(_db.CutPlan.FirstOrDefault(p => p.GUUID == HashId).CuttingPlan);
        }

        public async Task<string> GetNumCuttingPLan(string HashId)
        {
            if (string.IsNullOrEmpty(HashId)) return null;

            return _db.CutPlan.Where(p => p.GUUID == HashId)
                .Select(n => n.NumCut)
                .FirstOrDefault();
        }

        public async Task<bool> IsSaveCuttingPLan(string HashId)
        {
            if (string.IsNullOrEmpty(HashId)) return false;

            return _db.CutPlan.Where(p => p.GUUID == HashId)
                .Select(n => n.IsSave)
                .FirstOrDefault();
        }
        public async Task<string> InvoiceCuttingPLan(string HashId)
        {
            if (string.IsNullOrEmpty(HashId)) return null;

            return _db.CutPlan.Where(p => p.GUUID == HashId)
                .Select(n => n.Invoice)
                .FirstOrDefault();
        }

        public async Task<bool> SaveCuttangPlan(string HashId)
        {
            var cp = _db.CutPlan.Where(x => x.GUUID == HashId).FirstOrDefault();
            cp.IsSave = true;
            cp.SaveDate = DateTime.Now;
            return await _db.SaveChangesAsync() > 0;

        }

    }

    /***********GuilotineCut************/
    public class GuillotineAlgorithm
    {
        private const double BladeWidth = 3.2;
        private const double MinMargin = 0;

        public static CuttingPlan GuillotineCut(List<Sheet> sheets, List<Detail> details)
        {
            var bestPlan = new CuttingPlan { OptimizationAlgorithm = "Guillotine" };
            var allDetails = ExpandDetails(details);
            var remainingDetails = new List<Detail>(allDetails);

            foreach (var sheet in sheets)
            {
                // Пробуем разные стратегии сортировки
                var strategies = new[]
                {
                    "AreaDesc",      // По убыванию площади
                    "WidthDesc",     // По убыванию ширины  
                    "LengthDesc",    // По убыванию длины
                    "MaxSideDesc",   // По убыванию максимальной стороны
                    "Mixed"          // Смешанная стратегия
                };

                var bestSheetLayout = new SheetLayout { Sheet = sheet };
                int bestDetailsPlaced = 0;

                foreach (var strategy in strategies)
                {
                    var testLayout = TryPlaceOnSheet(sheet, allDetails, strategy);
                    int placedCount = testLayout.Details.Count;

                    if (placedCount > bestDetailsPlaced)
                    {
                        bestDetailsPlaced = placedCount;
                        bestSheetLayout = testLayout;
                    }
                }

                if (bestSheetLayout.Details.Any())
                {
                    bestSheetLayout.MaterialUsage = CalculateSheetMaterialUsage(sheet, bestSheetLayout);
                    bestSheetLayout.WastePercentage = 100 - (bestSheetLayout.MaterialUsage * 100);
                    CalculateCutLines(bestSheetLayout);
                    bestPlan.TotalCuts += bestSheetLayout.CutCount;
                    bestPlan.SheetLayouts.Add(bestSheetLayout);

                    // Удаляем размещенные детали
                    /*allDetails = allDetails.Except(bestSheetLayout.Details
                        .Select(d => allDetails.First(ad => ad.Name == d.Name)))
                        .ToList();*/
                    remainingDetails = RemovePlacedDetails(remainingDetails, bestSheetLayout);
                }
            }

            //bestPlan.UnplacedDetails = allDetails;
            bestPlan.UnplacedDetails = remainingDetails;
            CalculateTotals(bestPlan);

            return bestPlan;
        }

        private static List<Detail> RemovePlacedDetails(List<Detail> allDetails, SheetLayout layout)
        {
            var placedIds = layout.Details.Select(d => d.Id).ToHashSet();
            return allDetails.Where(d => !placedIds.Contains(d.Id)).ToList();
        }

        private static SheetLayout TryPlaceOnSheet(Sheet sheet, List<Detail> details, string strategy)
        {
            var layout = new SheetLayout { Sheet = sheet };
            var freeRects = new List<Rectangle>
        {
            new Rectangle(MinMargin, MinMargin,
                sheet.Width - 2 * MinMargin,
                sheet.Length - 2 * MinMargin)
        };

            var sortedDetails = SortDetails(details, strategy);

            foreach (var detail in sortedDetails)
            {
                TryPlaceDetailSmart(detail, freeRects, layout, sheet);
            }

            return layout;
        }

        private static bool TryPlaceDetailSmart(Detail detail, List<Rectangle> freeRects,
    SheetLayout layout, Sheet sheet)
        {
            // Пробуем оба варианта ориентации и выбираем лучший
            var options = new List<PlacementOption>();

            // Вариант без поворота
            TryAddPlacementOption(detail, false, freeRects, layout, sheet, options);

            // Вариант с поворотом
            if (detail.CanRotate)
            {
                TryAddPlacementOption(detail, true, freeRects, layout, sheet, options);
            }

            if (options.Count == 0) return false;

            // Выбираем вариант с наименьшими отходами
            var bestOption = options.OrderBy(o => o.Waste).First();

            // Размещаем деталь
            layout.Details.Add(new CutDetail
            {
                Length = bestOption.Length,
                Width = bestOption.Width,
                X = bestOption.X,
                Y = bestOption.Y,
                Rotated = bestOption.Rotated,
                Name = detail.Name,
                SheetId = sheet.Id,
                Id = detail.Id
            });

            // Обновляем свободное пространство
            UpdateFreeRects(freeRects, bestOption.Rect, bestOption.WidthWithBlade, bestOption.LengthWithBlade);

            return true;
        }

        private static void TryAddPlacementOption(Detail detail, bool rotate,
            List<Rectangle> freeRects, SheetLayout layout, Sheet sheet, List<PlacementOption> options)
        {
            double width = rotate ? detail.Length : detail.Width;
            double length = rotate ? detail.Width : detail.Length;

            double widthWithBlade = width + ((width < sheet.Width) ? BladeWidth : 0);
            double lengthWithBlade = length + ((length < sheet.Length) ? BladeWidth : 0);

            foreach (var rect in freeRects)
            {
                if (widthWithBlade <= rect.Width && lengthWithBlade <= rect.Height)
                {
                    if (rect.X + widthWithBlade > sheet.Width || rect.Y + lengthWithBlade > sheet.Length)
                        continue;

                    if (CheckOverlap(rect.X, rect.Y, width, length, layout.Details))
                        continue;

                    double waste = (rect.Width - widthWithBlade) * (rect.Height - lengthWithBlade);

                    options.Add(new PlacementOption
                    {
                        Width = width,
                        Length = length,
                        WidthWithBlade = widthWithBlade,
                        LengthWithBlade = lengthWithBlade,
                        X = rect.X,
                        Y = rect.Y,
                        Rotated = rotate,
                        Waste = waste,
                        Rect = rect
                    });
                }
            }
        }

        private static List<Detail> SortDetails(List<Detail> details, string strategy)
        {
            return strategy switch
            {
                "AreaDesc" => details.OrderByDescending(d => d.Length * d.Width).ToList(),
                "WidthDesc" => details.OrderByDescending(d => d.Width).ThenByDescending(d => d.Length).ToList(),
                "LengthDesc" => details.OrderByDescending(d => d.Length).ThenByDescending(d => d.Width).ToList(),
                "MaxSideDesc" => details.OrderByDescending(d => Math.Max(d.Length, d.Width)).ToList(),
                "Mixed" => details.OrderByDescending(d => Math.Max(d.Length, d.Width) * Math.Min(d.Length, d.Width)).ToList(),
                _ => details.OrderByDescending(d => d.Length * d.Width).ToList()
            };
        }

        private static void UpdateFreeRects(List<Rectangle> freeRects, Rectangle usedRect,
            double usedWidth, double usedHeight)
        {
            // Удаляем использованный прямоугольник
            freeRects.Remove(usedRect);

            // Добавляем новые свободные области
            if (usedRect.Width > usedWidth + MinMargin)
            {
                freeRects.Add(new Rectangle(
                    usedRect.X + usedWidth,
                    usedRect.Y,
                    usedRect.Width - usedWidth,
                    usedRect.Height
                ));
            }

            if (usedRect.Height > usedHeight + MinMargin)
            {
                freeRects.Add(new Rectangle(
                    usedRect.X,
                    usedRect.Y + usedHeight,
                    usedWidth,  // Ширина = ширине детали
                    usedRect.Height - usedHeight
                ));
            }

            // Очищаем от очень маленьких прямоугольников
            freeRects.RemoveAll(r => r.Width < MinMargin || r.Height < MinMargin);
        }

        private static bool CheckOverlap(double x, double y, double width, double height,
    List<CutDetail> existingDetails)
        {
            var newRect = new Rectangle(x, y, width, height);

            foreach (var existing in existingDetails)
            {
                var existingRect = new Rectangle(
                    existing.X, existing.Y, existing.Width, existing.Length);

                // Проверяем с зазором в 1мм для безопасности
                if (newRect.IntersectsWith(existingRect, 1.0))
                {
                    return true;
                }
            }

            return false;
        }


        private static List<Detail> ExpandDetails(List<Detail> details)
        {
            var expanded = new List<Detail>();
            foreach (var detail in details)
            {
                for (int i = 0; i < detail.Quantity; i++)
                {
                    expanded.Add(new Detail
                    {
                        Name = detail.Name,
                        Length = detail.Length,
                        Width = detail.Width,
                        CanRotate = detail.CanRotate,
                        Quantity = 1
                    });
                }
            }
            return expanded;
        }

        private static double CalculateSheetMaterialUsage(Sheet sheet, SheetLayout layout)
        {
            if (layout.Details.Count == 0) return 0;

            double sheetArea = sheet.Width * sheet.Length;
            double usedArea = layout.Details.Sum(d => d.Width * d.Length);
            return usedArea / sheetArea;
        }

        private static void CalculateTotals(CuttingPlan plan)
        {
            double totalSheetArea = plan.SheetLayouts.Sum(sl => sl.Sheet.Width * sl.Sheet.Length);
            double totalUsedArea = plan.SheetLayouts.Sum(sl => sl.Details.Sum(d => d.Width * d.Length));

            if (totalSheetArea > 0)
            {
                plan.TotalMaterialUsage = totalUsedArea / totalSheetArea;
                plan.TotalWastePercentage = 100 - (plan.TotalMaterialUsage * 100);
            }
        }

        private static void CalculateCutLines(SheetLayout layout, double bladeWidth = 3.2)
        {
            layout.CutLines = new List<CutLine>();
            var details = layout.Details.OrderBy(d => d.Y).ThenBy(d => d.X).ToList();

            if (!details.Any()) return;

            // Собираем все уникальные координаты для резов
            var cutX = new SortedSet<double>();
            var cutY = new SortedSet<double>();

            // Добавляем границы деталей
            foreach (var detail in details)
            {
                // Правая граница детали + отступ для реза
                cutX.Add(detail.X + detail.Width + bladeWidth);
                // Нижняя граница детали + отступ для реза
                cutY.Add(detail.Y + detail.Length + bladeWidth);
            }

            // Добавляем границы листа
            cutX.Add(0);
            cutX.Add(layout.Sheet.Width);
            cutY.Add(0);
            cutY.Add(layout.Sheet.Length);

            // Создаем резы между деталями (только необходимые)
            CreateVerticalCutsBetweenDetails(layout, cutX.ToList(), details, bladeWidth);
            CreateHorizontalCutsBetweenDetails(layout, cutY.ToList(), details, bladeWidth);

            layout.CutCount = layout.CutLines.Count;
        }

        private static void CreateVerticalCutsBetweenDetails(SheetLayout layout, List<double> xCoordinates,
            List<CutDetail> details, double bladeWidth)
        {
            for (int i = 0; i < xCoordinates.Count - 1; i++)
            {
                double x1 = xCoordinates[i];
                double x2 = xCoordinates[i + 1];

                // Пропускаем, если это не промежуток между деталями
                if (x2 - x1 <= bladeWidth + 0.1) continue;

                // Находим диапазон Y для этого вертикального промежутка
                var yRange = GetVerticalCutRange(x1, x2, details, layout.Sheet.Length);

                /*if (yRange.HasValue)
                {
                    layout.CutLines.Add(new CutLine
                    {
                        X1 = x1,
                        Y1 = yRange.Value.Start,
                        X2 = x1,
                        Y2 = yRange.Value.End,
                        Type = "vertical",
                        IsVisible = true
                    });
                }*/
            }
        }

        private static void CreateHorizontalCutsBetweenDetails(SheetLayout layout, List<double> yCoordinates,
            List<CutDetail> details, double bladeWidth)
        {
            for (int i = 0; i < yCoordinates.Count - 1; i++)
            {
                double y1 = yCoordinates[i];
                double y2 = yCoordinates[i + 1];

                // Пропускаем, если это не промежуток между деталями
                if (y2 - y1 <= bladeWidth + 0.1) continue;

                // Находим диапазон X для этого горизонтального промежутка
                var xRange = GetHorizontalCutRange(y1, y2, details, layout.Sheet.Width);

                /*if (xRange.HasValue)
                {
                    layout.CutLines.Add(new CutLine
                    {
                        X1 = xRange.Value.Start,
                        Y1 = y1,
                        X2 = xRange.Value.End,
                        Y2 = y1,
                        Type = "horizontal",
                        IsVisible = true
                    });
                }*/
            }
        }

        private static (double Start, double End)? GetVerticalCutRange(double x1, double x2,
            List<CutDetail> details, double sheetHeight)
        {
            double startY = 0;
            double endY = sheetHeight;
            bool foundGap = false;

            // Находим максимальный непрерывный промежуток по Y
            for (double y = 0; y <= sheetHeight; y += 1)
            {
                bool hasDetail = details.Any(d =>
                    d.X <= x2 && d.X + d.Width >= x1 && // Пересечение по X
                    d.Y <= y && d.Y + d.Length >= y);   // Пересечение по Y

                if (!hasDetail)
                {
                    if (!foundGap)
                    {
                        startY = y;
                        foundGap = true;
                    }
                    endY = y;
                }
                else if (foundGap)
                {
                    // Заканчиваем промежуток, когда встречаем деталь
                    break;
                }
            }

            return foundGap ? (startY, endY) : null;
        }

        private static (double Start, double End)? GetHorizontalCutRange(double y1, double y2,
            List<CutDetail> details, double sheetWidth)
        {
            double startX = 0;
            double endX = sheetWidth;
            bool foundGap = false;

            // Находим максимальный непрерывный промежуток по X
            for (double x = 0; x <= sheetWidth; x += 1)
            {
                bool hasDetail = details.Any(d =>
                    d.Y <= y2 && d.Y + d.Length >= y1 && // Пересечение по Y
                    d.X <= x && d.X + d.Width >= x);     // Пересечение по X

                if (!hasDetail)
                {
                    if (!foundGap)
                    {
                        startX = x;
                        foundGap = true;
                    }
                    endX = x;
                }
                else if (foundGap)
                {
                    // Заканчиваем промежуток, когда встречаем деталь
                    break;
                }
            }

            return foundGap ? (startX, endX) : null;
        }

        // Альтернативный упрощенный метод для вашего примера
        public static void CalculateSimpleCuts(SheetLayout layout, double bladeWidth = 3.2)
        {
            layout.CutLines = new List<CutLine>();
            var details = layout.Details.OrderBy(d => d.Y).ThenBy(d => d.X).ToList();

            if (!details.Any()) return;

            // Группируем детали по строкам и колонкам
            var rows = details.GroupBy(d => d.Y).OrderBy(g => g.Key);
            var columns = details.GroupBy(d => d.X).OrderBy(g => g.Key);

            // Вертикальные резы между колонками
            foreach (var columnGroup in columns)
            {
                var rightmostDetail = columnGroup.OrderByDescending(d => d.X + d.Width).First();
                double cutX = rightmostDetail.X + rightmostDetail.Width + bladeWidth;

                if (cutX < layout.Sheet.Width)
                {
                    // Находим Y-диапазон для реза
                    double minY = columnGroup.Min(d => d.Y);
                    double maxY = columnGroup.Max(d => d.Y + d.Length);

                    /*layout.CutLines.Add(new CutLine
                    {
                        X1 = cutX,
                        Y1 = minY,
                        X2 = cutX,
                        Y2 = maxY,
                        Type = "vertical",
                        IsVisible = true
                    });*/
                }
            }

            // Горизонтальные резы между строками
            foreach (var rowGroup in rows)
            {
                var bottomDetail = rowGroup.OrderByDescending(d => d.Y + d.Length).First();
                double cutY = bottomDetail.Y + bottomDetail.Length + bladeWidth;

                if (cutY < layout.Sheet.Length)
                {
                    // Находим X-диапазон для реза
                    double minX = rowGroup.Min(d => d.X);
                    double maxX = rowGroup.Max(d => d.X + d.Width);

                    /*layout.CutLines.Add(new CutLine
                    {
                        X1 = minX,
                        Y1 = cutY,
                        X2 = maxX,
                        Y2 = cutY,
                        Type = "horizontal",
                        IsVisible = true
                    });*/
                }
            }

            layout.CutCount = layout.CutLines.Count;
        }



    }

    public class GuillotineAlgorithm2
    {
        private const double BladeWidth = 3.2;  // Ширина реза
        private const double MinMargin = 0;     // Минимальный зазор

        public static CuttingPlan GuillotineCut(List<Sheet> sheets, List<Detail> details)
        {
            var bestPlan = new CuttingPlan { OptimizationAlgorithm = "Guillotine" };
            var allDetails = ExpandDetails(details);
            var remainingDetails = new List<Detail>(allDetails);

            foreach (var sheet in sheets)
            {
                // Пробуем разные стратегии — включая "SmallFirst" и "AreaDesc"
                var strategies = new[]
                {
                    "AreaDesc",      // По убыванию площади
                    "WidthDesc",     // По убыванию ширины  
                    "LengthDesc",    // По убыванию длины
                    "MaxSideDesc",   // По убыванию максимальной стороны
                    "Mixed"          // Смешанная стратегия
                };

                var bestSheetLayout = new SheetLayout { Sheet = sheet };
                int bestDetailsPlaced = 0;

                foreach (var strategy in strategies)
                {
                    var testLayout = TryPlaceOnSheet(sheet, allDetails, strategy);
                    int placedCount = testLayout.Details.Count;

                    if (placedCount > bestDetailsPlaced)
                    {
                        bestDetailsPlaced = placedCount;
                        bestSheetLayout = testLayout;
                    }
                }

                if (bestSheetLayout.Details.Any())
                {
                    bestSheetLayout.MaterialUsage = CalculateSheetMaterialUsage(sheet, bestSheetLayout);
                    bestSheetLayout.WastePercentage = 100 - (bestSheetLayout.MaterialUsage * 100);
                    bestPlan.TotalCuts += bestSheetLayout.CutLines.Count;
                    bestPlan.SheetLayouts.Add(bestSheetLayout);

                    remainingDetails = RemovePlacedDetails(remainingDetails, bestSheetLayout);
                }
            }

            bestPlan.UnplacedDetails = remainingDetails;
            CalculateTotals(bestPlan);

            return bestPlan;
        }

        private static List<Detail> RemovePlacedDetails(List<Detail> allDetails, SheetLayout layout)
        {
            var placedIds = layout.Details.Select(d => d.Id).ToHashSet();
            return allDetails.Where(d => !placedIds.Contains(d.Id)).ToList();
        }

        private static SheetLayout TryPlaceOnSheet(Sheet sheet, List<Detail> details, string strategy)
        {
            var layout = new SheetLayout { Sheet = sheet };
            var freeRects = new List<Rectangle>
            {
                new Rectangle(MinMargin, MinMargin,
                    sheet.Width - 2 * MinMargin,
                    sheet.Length - 2 * MinMargin)
            };

            var sortedDetails = SortDetails(details, strategy);

            foreach (var detail in sortedDetails)
            {
                var cuts = TryPlaceDetailSmart(detail, freeRects, layout, sheet);
                layout.CutLines.AddRange(cuts);
            }

            return layout;
        }

        private static List<CutLine> TryPlaceDetailSmart(Detail detail, List<Rectangle> freeRects,
            SheetLayout layout, Sheet sheet)
        {
            var options = new List<PlacementOption>();

            TryAddPlacementOption(detail, false, freeRects, layout, sheet, options);
            if (detail.CanRotate)
            {
                TryAddPlacementOption(detail, true, freeRects, layout, sheet, options);
            }

            if (!options.Any()) return new List<CutLine>();

            // Выбираем вариант с наименьшими отходами (максимальным остатком)
            var best = options.OrderBy(o => o.Waste).First();

            layout.Details.Add(new CutDetail
            {
                Name = detail.Name,
                Id = detail.Id,
                SheetId = sheet.Id,
                X = best.X,
                Y = best.Y,
                Width = best.Width,
                Length = best.Length,
                Rotated = best.Rotated
            });

            return UpdateFreeRectsAndReturnCuts(freeRects, best);
        }

        private static void TryAddPlacementOption(Detail detail, bool rotate,
            List<Rectangle> freeRects, SheetLayout layout, Sheet sheet, List<PlacementOption> options)
        {
            double width = rotate ? detail.Length : detail.Width;
            double length = rotate ? detail.Width : detail.Length;

            double widthWithBlade = width + (width < sheet.Width ? BladeWidth : 0);
            double lengthWithBlade = length + (length < sheet.Length ? BladeWidth : 0);

            foreach (var rect in freeRects)
            {
                if (widthWithBlade <= rect.Width && lengthWithBlade <= rect.Height)
                {
                    if (rect.X + widthWithBlade > sheet.Width || rect.Y + lengthWithBlade > sheet.Length)
                        continue;

                    if (CheckOverlap(rect.X, rect.Y, width, length, layout.Details))
                        continue;

                    // Приоритет — оставить крупный остаток
                    double remainingVertical = (rect.Width - widthWithBlade) * rect.Height;
                    double remainingHorizontal = (rect.Height - lengthWithBlade) * widthWithBlade;
                    double maxRemaining = Math.Max(remainingVertical, remainingHorizontal);

                    // Чем больше остаток — тем лучше вариант
                    double waste = -maxRemaining;

                    options.Add(new PlacementOption
                    {
                        Width = width,
                        Length = length,
                        WidthWithBlade = widthWithBlade,
                        LengthWithBlade = lengthWithBlade,
                        X = rect.X,
                        Y = rect.Y,
                        Rotated = rotate,
                        Waste = waste,
                        Rect = rect
                    });
                }
            }
        }

        private static List<Detail> SortDetails(List<Detail> details, string strategy)
        {
            return strategy switch
            {
                "AreaDesc" => details.OrderByDescending(d => d.Length * d.Width).ToList(),
                "WidthDesc" => details.OrderByDescending(d => d.Width).ThenByDescending(d => d.Length).ToList(),
                "LengthDesc" => details.OrderByDescending(d => d.Length).ThenByDescending(d => d.Width).ToList(),
                "MaxSideDesc" => details.OrderByDescending(d => Math.Max(d.Length, d.Width)).ToList(),
                "Mixed" => details.OrderByDescending(d => Math.Max(d.Length, d.Width) * Math.Min(d.Length, d.Width)).ToList(),
                _ => details.OrderByDescending(d => d.Length * d.Width).ToList()
            };
        }

        private static List<CutLine> UpdateFreeRectsAndReturnCuts(List<Rectangle> freeRects, PlacementOption placed)
        {
            var cuts = new List<CutLine>();

            // Находим родительский фрагмент
            var parent = freeRects.FirstOrDefault(r =>
                Math.Abs(r.X - placed.X) < 0.1 &&
                Math.Abs(r.Y - placed.Y) < 0.1 &&
                r.Width >= placed.WidthWithBlade - 0.1 &&
                r.Height >= placed.LengthWithBlade - 0.1);

            if (parent == null) return cuts;

            freeRects.Remove(parent);

            double placedRight = placed.X + placed.Width;
            double placedBottom = placed.Y + placed.Length;
            double cutX = placedRight + BladeWidth;
            double cutY = placedBottom + BladeWidth;

            bool canCutVertical = cutX < parent.X + parent.Width;
            bool canCutHorizontal = cutY < parent.Y + parent.Height;

            // --- Вертикальный рез ---
            if (canCutVertical)
            {
                cuts.Add(new CutLine
                {
                    X1 = cutX,
                    Y1 = parent.Y,
                    X2 = cutX,
                    Y2 = parent.Y + parent.Height,
                    Type = "vertical",
                    IsVisible = true
                });

                freeRects.Add(new Rectangle(
                    cutX,
                    parent.Y,
                    parent.X + parent.Width - cutX,
                    parent.Height
                ));
            }

            // --- Горизонтальный рез ---
            if (canCutHorizontal)
            {
                cuts.Add(new CutLine
                {
                    X1 = parent.X,
                    Y1 = cutY,
                    X2 = parent.X + parent.Width,
                    Y2 = cutY,
                    Type = "horizontal",
                    IsVisible = true
                });

                freeRects.Add(new Rectangle(
                    parent.X,
                    cutY,
                    parent.Width,
                    parent.Y + parent.Height - cutY
                ));
            }

            freeRects.RemoveAll(r => r.Width < MinMargin || r.Height < MinMargin);
            return cuts;
        }

        private static bool CheckOverlap(double x, double y, double width, double height,
            List<CutDetail> existingDetails)
        {
            var newRect = new Rectangle(x, y, width, height);

            foreach (var existing in existingDetails)
            {
                var existingRect = new Rectangle(existing.X, existing.Y, existing.Width, existing.Length);

                if (newRect.IntersectsWith(existingRect, 1.0)) // допуск 1 мм
                    return true;
            }

            return false;
        }

        private static List<Detail> ExpandDetails(List<Detail> details)
        {
            var expanded = new List<Detail>();
            foreach (var detail in details)
            {
                for (int i = 0; i < detail.Quantity; i++)
                {
                    expanded.Add(new Detail
                    {
                        Name = detail.Name,
                        Length = detail.Length,
                        Width = detail.Width,
                        CanRotate = detail.CanRotate,
                        Quantity = 1,
                        Id = detail.Id
                    });
                }
            }
            return expanded;
        }

        private static double CalculateSheetMaterialUsage(Sheet sheet, SheetLayout layout)
        {
            if (layout.Details.Count == 0) return 0;

            double sheetArea = sheet.Width * sheet.Length;
            double usedArea = layout.Details.Sum(d => d.Width * d.Length);
            return usedArea / sheetArea;
        }

        private static void CalculateTotals(CuttingPlan plan)
        {
            double totalSheetArea = plan.SheetLayouts.Sum(sl => sl.Sheet.Width * sl.Sheet.Length);
            double totalUsedArea = plan.SheetLayouts.Sum(sl => sl.Details.Sum(d => d.Width * d.Length));

            if (totalSheetArea > 0)
            {
                plan.TotalMaterialUsage = totalUsedArea / totalSheetArea;
                plan.TotalWastePercentage = 100 - (plan.TotalMaterialUsage * 100);
            }
        }
    }

    public class GuillotineAlgorithm3
    {
        private const double BladeWidth = 3.2;
        private const double MinMargin = 0;

        public static CuttingPlan GuillotineCut(List<Sheet> sheets, List<Detail> details)
        {
            var bestPlan = new CuttingPlan { OptimizationAlgorithm = "Guillotine" };
            var allDetails = ExpandDetails(details);
            var remainingDetails = new List<Detail>(allDetails);

            foreach (var sheet in sheets)
            {
                // Пробуем разные стратегии сортировки
                var strategies = new[]
                {
                    //"AreaDesc",      // По убыванию площади
                    //"WidthDesc",     // По убыванию ширины  
                    //"LengthDesc",    // По убыванию длины
                    //"MaxSideDesc",   // По убыванию максимальной стороны
                    //"Mixed",          // Смешанная стратегия Добавить сортировку, сначала маленькие, если помещаются на одну полоску (резы приритетно вертикальные)
                    "Smart"
                };

                var bestSheetLayout = new SheetLayout { Sheet = sheet };
                int bestDetailsPlaced = 0;

                foreach (var strategy in strategies)
                {
                    var testLayout = TryPlaceOnSheet(sheet, remainingDetails, strategy);
                    int placedCount = testLayout.Details.Count;

                    if (placedCount > bestDetailsPlaced)
                    {
                        bestDetailsPlaced = placedCount;
                        bestSheetLayout = testLayout;
                    }
                }

                bestSheetLayout = TryPlaceOnSheet(sheet, remainingDetails, "Smart");

                if (bestSheetLayout.Details.Any())
                {
                    bestSheetLayout.MaterialUsage = CalculateSheetMaterialUsage(sheet, bestSheetLayout);
                    bestSheetLayout.WastePercentage = 100 - (bestSheetLayout.MaterialUsage * 100);
                    CalculateUniversalCuts(bestSheetLayout);
                    bestPlan.TotalCuts += bestSheetLayout.CutCount;
                    bestPlan.SheetLayouts.Add(bestSheetLayout);

                    // Удаляем размещенные детали
                    /*allDetails = allDetails.Except(bestSheetLayout.Details
                        .Select(d => allDetails.First(ad => ad.Name == d.Name)))
                        .ToList();*/
                    remainingDetails = RemovePlacedDetails(remainingDetails, bestSheetLayout);
                }
            }

            //bestPlan.UnplacedDetails = allDetails;
            bestPlan.UnplacedDetails = remainingDetails;
            CalculateTotals(bestPlan);

            return bestPlan;
        }

        private static List<Detail> RemovePlacedDetails(List<Detail> allDetails, SheetLayout layout)
        {
            var placedIds = layout.Details.Select(d => d.Id).ToHashSet();
            return allDetails.Where(d => !placedIds.Contains(d.Id)).ToList();
        }

        private static SheetLayout TryPlaceOnSheet(Sheet sheet, List<Detail> details, string strategy)
        {
            var layout = new SheetLayout { Sheet = sheet };
            var freeRects = new List<Rectangle>
        {
            new Rectangle(MinMargin, MinMargin,
                sheet.Width - 2 * MinMargin,
                sheet.Length - 2 * MinMargin)
        };

            var sortedDetails = SortDetails(details, strategy);

            foreach (var detail in sortedDetails)
            {
                TryPlaceDetailSmart(detail, freeRects, layout, sheet);
            }

            return layout;
        }

        private static bool TryPlaceDetailSmart(Detail detail, List<Rectangle> freeRects,
    SheetLayout layout, Sheet sheet)
        {
            // Пробуем оба варианта ориентации и выбираем лучший
            var options = new List<PlacementOption>();

            // Вариант без поворота
            TryAddPlacementOption(detail, false, freeRects, layout, sheet, options);

            // Вариант с поворотом
            if (detail.CanRotate)
            {
                TryAddPlacementOption(detail, true, freeRects, layout, sheet, options);
            }

            if (options.Count == 0) return false;

            // Выбираем вариант с наименьшими отходами
            var bestOption = options.OrderBy(o => o.Waste).First();

            // Размещаем деталь
            layout.Details.Add(new CutDetail
            {
                Length = bestOption.Length,
                Width = bestOption.Width,
                X = bestOption.X,
                Y = bestOption.Y,
                Rotated = bestOption.Rotated,
                Name = detail.Name,
                SheetId = sheet.Id,
                Id = detail.Id
            });

            // Обновляем свободное пространство
            UpdateFreeRects(freeRects, bestOption.Rect, bestOption.WidthWithBlade, bestOption.LengthWithBlade);

            return true;
        }

        private static void TryAddPlacementOption(Detail detail, bool rotate,
            List<Rectangle> freeRects, SheetLayout layout, Sheet sheet, List<PlacementOption> options)
        {
            double width = rotate ? detail.Length : detail.Width;
            double length = rotate ? detail.Width : detail.Length;

            double widthWithBlade = width + ((width < sheet.Width) ? BladeWidth : 0);
            double lengthWithBlade = length + ((length < sheet.Length) ? BladeWidth : 0);

            foreach (var rect in freeRects)
            {
                if (widthWithBlade <= rect.Width && lengthWithBlade <= rect.Height)
                {
                    if (rect.X + widthWithBlade > sheet.Width || rect.Y + lengthWithBlade > sheet.Length)
                        continue;

                    if (CheckOverlap(rect.X, rect.Y, width, length, layout.Details))
                        continue;

                    double waste = (rect.Width - widthWithBlade) * (rect.Height - lengthWithBlade);

                    options.Add(new PlacementOption
                    {
                        Width = width,
                        Length = length,
                        WidthWithBlade = widthWithBlade,
                        LengthWithBlade = lengthWithBlade,
                        X = rect.X,
                        Y = rect.Y,
                        Rotated = rotate,
                        Waste = waste,
                        Rect = rect
                    });
                }
            }
        }

        private static List<Detail> SortDetails(List<Detail> details, string strategy)
        {
            return strategy switch
            {
                "AreaDesc" => details.OrderByDescending(d => d.Length * d.Width).ToList(),
                "WidthDesc" => details.OrderByDescending(d => d.Width).ThenByDescending(d => d.Length).ToList(),
                "LengthDesc" => details.OrderByDescending(d => d.Length).ThenByDescending(d => d.Width).ToList(),
                "MaxSideDesc" => details.OrderByDescending(d => Math.Max(d.Length, d.Width)).ToList(),
                "Mixed" => details.OrderByDescending(d => Math.Max(d.Length, d.Width) * Math.Min(d.Length, d.Width)).ToList(),
                "Smart" => SortDetailsSmart(details),
                _ => details.OrderByDescending(d => d.Length * d.Width).ToList()
            };
        }

        private static List<Detail> SortDetailsSmart(List<Detail> details)
        {
            if (!details.Any()) return new List<Detail>();

            // Эвристика: мелкие детали — меньше 50% от средней площади
            double avgArea = details.Average(d => d.Width * d.Length);
            double smallThreshold = avgArea * 0.5;

            var smallDetails = details.Where(d => d.Width * d.Length < smallThreshold).ToList();
            var largeDetails = details.Where(d => d.Width * d.Length >= smallThreshold).ToList();

            // Если мелких > 30% — сначала мелкие по возрастанию, потом крупные по возрастанию
            if (smallDetails.Count > details.Count * 0.3)
            {
                var sortedSmall = smallDetails.OrderBy(d => d.Width * d.Length).ToList();
                var sortedLarge = largeDetails.OrderBy(d => d.Width * d.Length).ToList();
                return sortedSmall.Concat(sortedLarge).ToList();
            }
            else
            {
                // Мало мелких — сначала крупные по убыванию, потом мелкие по возрастанию
                var sortedLarge = largeDetails.OrderByDescending(d => d.Width * d.Length).ToList();
                var sortedSmall = smallDetails.OrderBy(d => d.Width * d.Length).ToList();
                return sortedLarge.Concat(sortedSmall).ToList();
            }
        }

        private static void UpdateFreeRects(List<Rectangle> freeRects, Rectangle usedRect,
            double usedWidth, double usedHeight)
        {
            // Удаляем использованный прямоугольник
            freeRects.Remove(usedRect);

            // Добавляем новые свободные области
            if (usedRect.Width > usedWidth + MinMargin)
            {
                freeRects.Add(new Rectangle(
                    usedRect.X + usedWidth,
                    usedRect.Y,
                    usedRect.Width - usedWidth,
                    usedRect.Height
                ));
            }

            if (usedRect.Height > usedHeight + MinMargin)
            {
                freeRects.Add(new Rectangle(
                    usedRect.X,
                    usedRect.Y + usedHeight,
                    usedWidth,  // Ширина = ширине детали
                    usedRect.Height - usedHeight
                ));
            }

            // Очищаем от очень маленьких прямоугольников
            freeRects.RemoveAll(r => r.Width < MinMargin || r.Height < MinMargin);
        }

        private static bool CheckOverlap(double x, double y, double width, double height,
    List<CutDetail> existingDetails)
        {
            var newRect = new Rectangle(x, y, width, height);

            foreach (var existing in existingDetails)
            {
                var existingRect = new Rectangle(
                    existing.X, existing.Y, existing.Width, existing.Length);

                // Проверяем с зазором в 1мм для безопасности
                if (newRect.IntersectsWith(existingRect, 1.0))
                {
                    return true;
                }
            }

            return false;
        }


        private static List<Detail> ExpandDetails(List<Detail> details)
        {
            var expanded = new List<Detail>();
            foreach (var detail in details)
            {
                for (int i = 0; i < detail.Quantity; i++)
                {
                    expanded.Add(new Detail
                    {
                        Name = detail.Name,
                        Length = detail.Length,
                        Width = detail.Width,
                        CanRotate = detail.CanRotate,
                        Quantity = 1
                    });
                }
            }
            return expanded;
        }

        private static double CalculateSheetMaterialUsage(Sheet sheet, SheetLayout layout)
        {
            if (layout.Details.Count == 0) return 0;

            double sheetArea = sheet.Width * sheet.Length;
            double usedArea = layout.Details.Sum(d => d.Width * d.Length);
            return usedArea / sheetArea;
        }

        private static void CalculateTotals(CuttingPlan plan)
        {
            double totalSheetArea = plan.SheetLayouts.Sum(sl => sl.Sheet.Width * sl.Sheet.Length);
            double totalUsedArea = plan.SheetLayouts.Sum(sl => sl.Details.Sum(d => d.Width * d.Length));

            if (totalSheetArea > 0)
            {
                plan.TotalMaterialUsage = totalUsedArea / totalSheetArea;
                plan.TotalWastePercentage = 100 - (plan.TotalMaterialUsage * 100);
            }
        }

        public static void CalculateUniversalCuts(SheetLayout layout, double bladeWidth = 3.2)
        {
            layout.CutLines = new List<CutLine>();
            var details = layout.Details.Select(d => new CutDetail
            {
                X = d.X,
                Y = d.Y,
                Width = d.Width,
                Length = d.Length,
                Rotated = d.Rotated,
                Name = d.Name,
                SheetId = d.SheetId,
                Id = d.Id
            }).ToList();

            if (!details.Any()) return;

            var initialFragment = new CutFragment
            {
                X = 0,
                Y = 0,
                Width = layout.Sheet.Width,
                Height = layout.Sheet.Length,
                Details = details
            };

            RecursiveGuillotineCut(initialFragment, layout.CutLines, bladeWidth);

            layout.CutCount = layout.CutLines.Count;
        }

        private static void RecursiveGuillotineCut(CutFragment fragment, List<CutLine> cutLines, double bladeWidth)
        {
            if (fragment.Details.Count == 1)
            {
                GenerateFinalCuts(fragment, cutLines, bladeWidth);
                return;
            }
            if (fragment.Details.Count == 0)
            {
                return;
            }

            var bestCut = FindBestGuillotineCut(fragment, bladeWidth);

            if (bestCut == null)
            {
                // Невозможно найти гильотинный рез — расположение негильотинное.
                // В теории, такого быть не должно, если события включают края деталей.
                // Но на всякий случай — просто выходим, не делая ничего.
                // (Можно добавить логирование ошибки)
                return;
            }

            // Добавляем рез
            cutLines.Add(bestCut.CutLine);

            // Делим фрагмент
            var (leftFrag, rightFrag) = SplitFragment(fragment, bestCut, bladeWidth);

            // Рекурсия
            RecursiveGuillotineCut(leftFrag, cutLines, bladeWidth);
            RecursiveGuillotineCut(rightFrag, cutLines, bladeWidth);
        }

        private static void GenerateFinalCuts(CutFragment fragment, List<CutLine> cutLines, double bladeWidth)
        {
            var detail = fragment.Details[0];
            double x = detail.X;
            double y = detail.Y;
            double w = detail.Width;
            double h = detail.Length;
            double fragX = fragment.X;
            double fragY = fragment.Y;
            double fragW = fragment.Width;
            double fragH = fragment.Height;

            const double eps = 0.1;

            // Рез слева от детали (если есть зазор)
            if (x > fragX + eps)
            {
                cutLines.Add(new CutLine
                {
                    X1 = RoundToTenth(x - bladeWidth / 2),
                    Y1 = RoundToTenth(fragY),
                    X2 = RoundToTenth(x + bladeWidth / 2),
                    Y2 = RoundToTenth(fragY + fragH),
                    Type = "vertical",
                    IsVisible = true
                });
            }

            // Рез справа от детали (если есть зазор)
            if (x + w < fragX + fragW - eps)
            {
                cutLines.Add(new CutLine
                {
                    X1 = RoundToTenth(x + w - bladeWidth / 2),
                    Y1 = RoundToTenth(fragY),
                    X2 = RoundToTenth(x + w + bladeWidth / 2),
                    Y2 = RoundToTenth(fragY + fragH),
                    Type = "vertical",
                    IsVisible = true
                });
            }

            // Рез сверху от детали (если есть зазор)
            if (y > fragY + eps)
            {
                cutLines.Add(new CutLine
                {
                    X1 = RoundToTenth(fragX),
                    Y1 = RoundToTenth(y - bladeWidth / 2),
                    X2 = RoundToTenth(fragX + fragW),
                    Y2 = RoundToTenth(y + bladeWidth / 2),
                    Type = "horizontal",
                    IsVisible = true
                });
            }

            // Рез снизу от детали (если есть зазор) ← именно он нужен для детали 5!
            if (y + h < fragY + fragH - eps)
            {
                cutLines.Add(new CutLine
                {
                    X1 = RoundToTenth(fragX),
                    Y1 = RoundToTenth(y + h - bladeWidth / 2),
                    X2 = RoundToTenth(fragX + fragW),
                    Y2 = RoundToTenth(y + h + bladeWidth / 2),
                    Type = "horizontal",
                    IsVisible = true
                });
            }
        }
        private static GuillotineCut FindBestGuillotineCut(CutFragment fragment, double bladeWidth)
        {
            /*var candidates = new List<GuillotineCut>();

            var verticalCuts = FindVerticalGuillotineCuts(fragment, bladeWidth);
            candidates.AddRange(verticalCuts);

            var horizontalCuts = FindHorizontalGuillotineCuts(fragment, bladeWidth);
            candidates.AddRange(horizontalCuts);

            if (!candidates.Any()) return null;

            // Жадно выбираем рез, отделяющий наибольшее количество деталей
            return candidates.OrderByDescending(c => GetSeparatedDetailsCount(fragment, c, bladeWidth)).First();*/
            // Сначала ищем все вертикальные резы
            var verticalCuts = FindVerticalGuillotineCuts(fragment, bladeWidth);

            // Если есть хоть один вертикальный — возвращаем первый (или любой)
            if (verticalCuts.Any())
            {
                return verticalCuts.First();
            }

            // Если вертикальных нет — ищем горизонтальные
            var horizontalCuts = FindHorizontalGuillotineCuts(fragment, bladeWidth);
            if (horizontalCuts.Any())
            {
                return horizontalCuts.First();
            }

            return null;
        }

        private static int GetSeparatedDetailsCount(CutFragment fragment, GuillotineCut cut, double bladeWidth)
        {
            var (left, right) = SplitFragment(fragment, cut, bladeWidth);

            int leftCount = left.Details.Count;
            int rightCount = right.Details.Count;

            // 🔥 Приоритет 1: если рез — это "подрезка" по краю детали (вертикальный или горизонтальный)
            if (cut.IsVertical)
            {
                foreach (var detail in fragment.Details)
                {
                    // Если рез слева от детали
                    if (Math.Abs(cut.Position - bladeWidth / 2 - detail.X) < 0.1)
                        return 20000;

                    // Если рез справа от детали
                    if (Math.Abs(cut.Position + bladeWidth / 2 - (detail.X + detail.Width)) < 0.1)
                        return 20000;
                }
            }
            else
            {
                foreach (var detail in fragment.Details)
                {
                    // Если рез сверху от детали
                    if (Math.Abs(cut.Position - bladeWidth / 2 - detail.Y) < 0.1)
                        return 20000;

                    // Если рез снизу от детали
                    if (Math.Abs(cut.Position + bladeWidth / 2 - (detail.Y + detail.Length)) < 0.1)
                        return 20000;
                }
            }

            // 🔥 Приоритет 2: если рез изолирует одну деталь
            if ((leftCount == 1 && rightCount == 0) || (rightCount == 1 && leftCount == 0))
            {
                return 10000;
            }

            // 🔥 Приоритет 3: вертикальный рез, разделяющий фрагмент на две НЕПУСТЫЕ части
            if (cut.IsVertical && leftCount > 0 && rightCount > 0)
            {
                return 9000;
            }

            // 🔥 Приоритет 4: горизонтальный рез, разделяющий фрагмент на две НЕПУСТЫЕ части
            if (!cut.IsVertical && leftCount > 0 && rightCount > 0)
            {
                return 8000;
            }

            // По умолчанию — максимум деталей с одной стороны
            return Math.Max(leftCount, rightCount);

        }

        private static List<GuillotineCut> FindVerticalGuillotineCuts(CutFragment fragment, double bladeWidth)
        {
            var cuts = new List<GuillotineCut>();
            var events = new SortedSet<double>();

            /*double minCut = fragment.X + bladeWidth / 2;
            double maxCut = fragment.X + fragment.Width - bladeWidth / 2;

            events.Add(minCut);
            events.Add(maxCut);*/

            foreach (var detail in fragment.Details)
            {
                if (detail.X > fragment.X + 0.1)
                    events.Add(detail.X - bladeWidth / 2);

                if (detail.X + detail.Width < fragment.X + fragment.Width - 0.1)
                    events.Add(detail.X + detail.Width + bladeWidth / 2);
            }

            const double eps = 0.01;

            foreach (var x in events)
            {
                //if (x <= minCut || x >= maxCut)
                if (x <= fragment.X + bladeWidth / 2 || x >= fragment.X + fragment.Width - bladeWidth / 2)
                    continue;

                var leftDetails = fragment.Details.Where(d => d.X + d.Width <= x + eps).ToList();
                var rightDetails = fragment.Details.Where(d => d.X >= x - eps).ToList();

                // Проверка: не проходит ли рез через деталь?
                bool overlapping = fragment.Details.Any(d => d.X < x && d.X + d.Width > x);
                if (overlapping)
                    continue;

                cuts.Add(new GuillotineCut
                {
                    IsVertical = true,
                    Position = x,
                    CutLine = new CutLine
                    {
                        X1 = RoundToTenth(x - bladeWidth / 2),
                        Y1 = RoundToTenth(fragment.Y),
                        X2 = RoundToTenth(x + bladeWidth / 2),
                        Y2 = RoundToTenth(fragment.Y + fragment.Height),
                        Type = "vertical",
                        IsVisible = true
                    }
                });
            }

            return cuts;
        }

        private static List<GuillotineCut> FindHorizontalGuillotineCuts(CutFragment fragment, double bladeWidth)
        {
            var cuts = new List<GuillotineCut>();
            var events = new SortedSet<double>();

            /*double minCut = fragment.Y + bladeWidth / 2;
            double maxCut = fragment.Y + fragment.Height - bladeWidth / 2;

            events.Add(minCut);
            events.Add(maxCut);*/

            foreach (var detail in fragment.Details)
            {
                if (detail.Y > fragment.Y + 0.1)
                    events.Add(detail.Y - bladeWidth / 2);

                if (detail.Y + detail.Length < fragment.Y + fragment.Height - 0.1)
                    events.Add(detail.Y + detail.Length + bladeWidth / 2);
            }

            const double eps = 0.01;

            foreach (var y in events)
            {
                //if (y <= minCut || y >= maxCut)
                if (y <= fragment.Y + bladeWidth / 2 || y >= fragment.Y + fragment.Height - bladeWidth / 2)
                    continue;

                var topDetails = fragment.Details.Where(d => d.Y + d.Length <= y + eps).ToList();
                var bottomDetails = fragment.Details.Where(d => d.Y >= y - eps).ToList();

                // Проверка: не проходит ли рез через деталь?
                bool overlapping = fragment.Details.Any(d => d.Y < y && d.Y + d.Length > y);
                if (overlapping)
                    continue;

                cuts.Add(new GuillotineCut
                {
                    IsVertical = false,
                    Position = y,
                    CutLine = new CutLine
                    {
                        X1 = RoundToTenth(fragment.X),
                        Y1 = RoundToTenth(y - bladeWidth / 2),
                        X2 = RoundToTenth(fragment.X + fragment.Width),
                        Y2 = RoundToTenth(y + bladeWidth / 2),
                        Type = "horizontal",
                        IsVisible = true
                    }
                });
            }

            return cuts;
        }

        private static (CutFragment Left, CutFragment Right) SplitFragment(CutFragment fragment, GuillotineCut cut, double bladeWidth)
        {
            const double eps = 0.01;

            if (cut.IsVertical)
            {
                var left = new CutFragment
                {
                    X = fragment.X,
                    Y = fragment.Y,
                    Width = RoundToTenth(cut.Position - bladeWidth / 2 - fragment.X),
                    Height = fragment.Height,
                    Details = fragment.Details.Where(d =>
                        d.X + d.Width <= cut.Position + eps &&
                        d.Y >= fragment.Y - eps &&
                        d.Y + d.Length <= fragment.Y + fragment.Height + eps
                    ).ToList()
                };

                var right = new CutFragment
                {
                    X = RoundToTenth(cut.Position + bladeWidth / 2),
                    Y = fragment.Y,
                    Width = RoundToTenth(fragment.X + fragment.Width - (cut.Position + bladeWidth / 2)),
                    Height = fragment.Height,
                    Details = fragment.Details.Where(d =>
                        d.X >= cut.Position - eps &&
                        d.Y >= fragment.Y - eps &&
                        d.Y + d.Length <= fragment.Y + fragment.Height + eps
                    ).ToList()
                };

                return (left, right);
            }
            else
            {
                var top = new CutFragment
                {
                    X = fragment.X,
                    Y = fragment.Y,
                    Width = fragment.Width,
                    Height = RoundToTenth(cut.Position - bladeWidth / 2 - fragment.Y),
                    Details = fragment.Details.Where(d =>
                        d.Y + d.Length <= cut.Position + eps &&
                        d.X >= fragment.X - eps &&
                        d.X + d.Width <= fragment.X + fragment.Width + eps
                    ).ToList()
                };

                var bottom = new CutFragment
                {
                    X = fragment.X,
                    Y = RoundToTenth(cut.Position + bladeWidth / 2),
                    Width = fragment.Width,
                    Height = RoundToTenth(fragment.Y + fragment.Height - (cut.Position + bladeWidth / 2)),
                    Details = fragment.Details.Where(d =>
                        d.Y >= cut.Position - eps &&
                        d.X >= fragment.X - eps &&
                        d.X + d.Width <= fragment.X + fragment.Width + eps
                    ).ToList()
                };

                return (top, bottom);
            }
        }

        private static double RoundToTenth(double value)
        {
            return Math.Round(value, 2);
        }

    }
}
