using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Cutter.Data;

namespace Cutter.Services
{
    public interface IStoreAdminService
    {
        Task<StoreAdminDashboardData> GetDashboardDataAsync(string userId, DateTime startDate, DateTime endDate);
    }

    public class StoreAdminService : IStoreAdminService
    {
        private readonly ApplicationDbContext _dbContext;

        public StoreAdminService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StoreAdminDashboardData> GetDashboardDataAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("Магазин не найден");

            Guid storeId = user.StoreId;
            var endDateEndOfDay = endDate.AddDays(1).AddTicks(-1);

            var plans = await _dbContext.CutPlan
                .Include(p => p.User)
                .Where(cp => cp.IsSave &&
                             cp.CreateDate >= startDate &&
                             cp.CreateDate <= endDateEndOfDay &&
                             cp.User != null &&
                             cp.User.StoreId == storeId)
                .ToListAsync();

            var result = new StoreAdminDashboardData();
            var sheetDict = new Dictionary<string, SheetUsageStat>();
            var dailySheetUsage = new Dictionary<DateTime, int>();

            int totalPlans = plans.Count;
            int totalSheetsUsed = 0;
            decimal totalMaterialCost = 0;

            foreach (var dbPlan in plans)
            {
                if (string.IsNullOrEmpty(dbPlan.CuttingPlan)) continue;

                try
                {
                    var cuttingPlans = JsonConvert.DeserializeObject<List<CuttingPlan>>(dbPlan.CuttingPlan);
                    if (cuttingPlans == null) continue;

                    foreach (var cp in cuttingPlans)
                    {
                        foreach (var layout in cp.SheetLayouts)
                        {
                            var sheet = layout.Sheet;
                            if (sheet == null) continue;

                            totalSheetsUsed++;
                            var sheetCost = (decimal)sheet.Price;
                            totalMaterialCost += sheetCost;

                            var key = $"{sheet.SKU}_{sheet.Price}";
                            if (!sheetDict.ContainsKey(key))
                            {
                                sheetDict[key] = new SheetUsageStat
                                {
                                    Name = string.IsNullOrWhiteSpace(sheet.Name) ? sheet.SKU : sheet.Name,
                                    SKU = sheet.SKU,
                                    PricePerSheet = sheetCost
                                };
                            }
                            sheetDict[key].Quantity += 1;
                            sheetDict[key].TotalAmount += sheetCost;

                            var planDate = dbPlan.CreateDate.Date;
                            if (!dailySheetUsage.ContainsKey(planDate))
                            {
                                dailySheetUsage[planDate] = 0;
                            }
                            dailySheetUsage[planDate] += 1;
                        }
                    }
                }
                catch { }
            }

            var sortedSheets = sheetDict.Values.OrderByDescending(s => s.TotalAmount).ToList();

            result.Stats = new StoreAdminStats
            {
                TotalPlans = totalPlans,
                TotalSheetsUsed = totalSheetsUsed,
                TotalMaterialCost = totalMaterialCost,
                UniqueSheetTypes = sortedSheets.Count,
                TopSheetName = sortedSheets.FirstOrDefault()?.Name ?? "Нет данных"
            };

            result.SheetStats = sortedSheets;

            var sortedDaily = dailySheetUsage.OrderBy(d => d.Key).ToList();
            result.ChartLabels = sortedDaily.Select(d => d.Key.ToString("dd.MM")).ToList();
            result.ChartQtyData = sortedDaily.Select(d => d.Value).ToList();

            var topSheets = sortedSheets.Take(5).ToList();
            result.PieLabels = topSheets.Select(s => s.Name).ToList();
            result.PieCostData = topSheets.Select(s => s.TotalAmount).ToList();

            if (sortedSheets.Count > 5)
            {
                result.PieLabels.Add("Остальные");
                result.PieCostData.Add(sortedSheets.Skip(5).Sum(s => s.TotalAmount));
            }

            return result;
        }
    }

    public class StoreAdminDashboardData
    {
        public StoreAdminStats Stats { get; set; } = new();
        public List<SheetUsageStat> SheetStats { get; set; } = new();
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartQtyData { get; set; } = new();
        public List<string> PieLabels { get; set; } = new();
        public List<decimal> PieCostData { get; set; } = new();
    }

    public class StoreAdminStats
    {
        public int TotalPlans { get; set; }
        public int TotalSheetsUsed { get; set; }
        public decimal TotalMaterialCost { get; set; }
        public int UniqueSheetTypes { get; set; }
        public string TopSheetName { get; set; } = string.Empty;
    }

    public class SheetUsageStat
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PricePerSheet { get; set; }
        public decimal TotalAmount { get; set; }
    }
}