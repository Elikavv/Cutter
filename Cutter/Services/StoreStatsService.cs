using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Cutter.Data;

namespace Cutter.Services
{
    public interface IStoreStatsService
    {
        Task<StoreStatsResult> GetSheetStatsAsync(string userId, DateTime startDate, DateTime endDate);
    }

    public class StoreStatsService : IStoreStatsService
    {
        private readonly ApplicationDbContext _dbContext;

        public StoreStatsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StoreStatsResult> GetSheetStatsAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("Магазин не найден");

            Guid storeId = user.StoreId;
            var endDateEndOfDay = endDate.AddDays(1).AddTicks(-1);

            // ВАЖНО: Фильтруем строго по магазину админа!
            var plans = await _dbContext.CutPlan
                .Include(p => p.User)
                .Where(cp => cp.IsSave &&
                             cp.CreateDate >= startDate &&
                             cp.CreateDate <= endDateEndOfDay &&
                             cp.User != null &&
                             cp.User.StoreId == storeId)
                .ToListAsync();

            var sheetGroups = new Dictionary<string, SheetStatEntry>();
            int totalSheets = 0;
            decimal totalCost = 0;

            foreach (var plan in plans)
            {
                if (string.IsNullOrWhiteSpace(plan.CuttingPlan)) continue;

                List<CuttingPlan>? planList;
                try
                {
                    planList = JsonConvert.DeserializeObject<List<CuttingPlan>>(plan.CuttingPlan);
                }
                catch
                {
                    continue; // Пропускаем битые записи
                }

                if (planList == null) continue;

                foreach (var cp in planList)
                {
                    if (cp.SheetLayouts == null) continue;

                    foreach (var layout in cp.SheetLayouts)
                    {
                        var sheet = layout.Sheet;
                        if (sheet == null) continue;

                        var sheetName = string.IsNullOrWhiteSpace(sheet.Name) ? "Неизвестный лист" : sheet.Name;
                        var price = (decimal)sheet.Price;
                        var qty = sheet.Quantity;

                        var key = $"{sheetName}_{price}";

                        if (!sheetGroups.ContainsKey(key))
                        {
                            sheetGroups[key] = new SheetStatEntry
                            {
                                SheetName = sheetName,
                                Price = price
                            };
                        }

                        sheetGroups[key].TotalQuantity += qty;

                        totalSheets += qty;
                        totalCost += price * qty;
                    }
                }
            }

            var result = new StoreStatsResult
            {
                Entries = sheetGroups.Values.OrderByDescending(x => x.TotalSum).ToList(),
                TotalSheets = totalSheets,
                TotalCost = totalCost,
                UniqueTypes = sheetGroups.Count,
                AveragePricePerSheet = totalSheets > 0 ? totalCost / totalSheets : 0
            };

            return result;
        }
    }

    // === DTO ===
    public class StoreStatsResult
    {
        public List<SheetStatEntry> Entries { get; set; } = new();
        public int TotalSheets { get; set; }
        public decimal TotalCost { get; set; }
        public int UniqueTypes { get; set; }
        public decimal AveragePricePerSheet { get; set; }
    }

    public class SheetStatEntry
    {
        public string SheetName { get; set; } = "—";
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalSum => TotalQuantity * Price;
    }
}