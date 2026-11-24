using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Cutter.Data;

namespace Cutter.Services
{
    public interface IStoreStatsService
    {

        Task<StoreStatsResult> GetSheetStatsAsync(string userId, DateTime startDate, DateTime endDate,
            List<int>? selectedDepartmentIds = null, List<int>? selectedSubdepartmentIds = null);


        Task<DepartmentStatsResult> GetDepartmentStatsAsync(string userId, DateTime startDate, DateTime endDate,
            List<int>? selectedDepartmentIds = null, List<int>? selectedSubdepartmentIds = null);
    }

    public class StoreStatsService : IStoreStatsService
    {
        private readonly ApplicationDbContext _dbContext;

        public StoreStatsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StoreStatsResult> GetSheetStatsAsync(string userId, DateTime startDate, DateTime endDate,
            List<int>? selectedDepartmentIds = null, List<int>? selectedSubdepartmentIds = null)
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

            // Кэш: SKU и BarCode -> (SubdeptId, DeptId)
            var storeItemsList = await _dbContext.StoreItems
                .Include(si => si.Item)
                .Include(si => si.StoreSubdepartment)
                .Where(si => si.StoreId == storeId)
                .ToListAsync();

            var storeItemsCache = new Dictionary<string, (int? SubdeptId, int? DeptId)>();

            foreach (var si in storeItemsList)
            {
                var subdeptId = si.StoreSubdepartment?.Id;
                var deptId = si.StoreSubdepartment?.StoreDepartmentId;
                var value = (subdeptId, deptId);

                if (!string.IsNullOrEmpty(si.Item?.SKU) && !storeItemsCache.ContainsKey(si.Item.SKU))
                    storeItemsCache[si.Item.SKU] = value;
                if (!string.IsNullOrEmpty(si.Item?.BarCode) && !storeItemsCache.ContainsKey(si.Item.BarCode))
                    storeItemsCache[si.Item.BarCode] = value;
            }

            // === КЭШ НАЗВАНИЙ (загружаем через ToList, чтобы избежать ошибок трансляции EF Core) ===
            var deptsList = await _dbContext.StoreDepartments.Where(d => d.StoreId == storeId).ToListAsync();
            var deptNames = deptsList.ToDictionary(d => d.Id, d => d.Name);
            var validDeptIds = deptsList.Select(d => d.Id).ToList();

            var subdeptsList = await _dbContext.StoreSubdepartments.Where(sd => validDeptIds.Contains(sd.StoreDepartmentId)).ToListAsync();
            var subdeptNames = subdeptsList.ToDictionary(sd => sd.Id, sd => sd.Name);

            // === ЖЕЛЕЗОБЕТОННАЯ ЛОГИКА ФИЛЬТРАЦИИ ===
            HashSet<int>? effectiveDeptIds = null;
            if (selectedDepartmentIds != null)
            {
                effectiveDeptIds = new HashSet<int>(selectedDepartmentIds);

                if (selectedSubdepartmentIds != null && selectedSubdepartmentIds.Any())
                {
                    var parentDepts = await _dbContext.StoreSubdepartments
                        .Where(sd => selectedSubdepartmentIds.Contains(sd.Id))
                        .Select(sd => sd.StoreDepartmentId)
                        .ToListAsync();

                    foreach (var deptId in parentDepts)
                    {
                        if (deptId > 0) effectiveDeptIds.Add(deptId);
                    }
                }
            }

            bool isDeptFilterActive = effectiveDeptIds != null;
            bool isSubdeptFilterActive = selectedSubdepartmentIds != null;

            var sheetGroups = new Dictionary<string, SheetStatEntry>();
            int totalSheets = 0;
            decimal totalCost = 0;

            foreach (var plan in plans)
            {
                if (string.IsNullOrWhiteSpace(plan.CuttingPlan)) continue;

                List<CuttingPlan>? planList;
                try { planList = JsonConvert.DeserializeObject<List<CuttingPlan>>(plan.CuttingPlan); }
                catch { continue; }

                if (planList == null) continue;

                foreach (var cp in planList)
                {
                    if (cp.SheetLayouts == null) continue;

                    foreach (var layout in cp.SheetLayouts)
                    {
                        var sheet = layout.Sheet;
                        if (sheet == null) continue;

                        var itemKey = sheet.SKU ?? sheet.BarCode;

                        // 1. ФИЛЬТРАЦИЯ (твоя рабочая логика)
                        if (!string.IsNullOrEmpty(itemKey) && storeItemsCache.TryGetValue(itemKey, out var deptInfo))
                        {
                            if (isDeptFilterActive)
                            {
                                if (!deptInfo.DeptId.HasValue || !effectiveDeptIds!.Contains(deptInfo.DeptId.Value))
                                    continue;
                            }

                            if (isSubdeptFilterActive)
                            {
                                if (!deptInfo.SubdeptId.HasValue || !selectedSubdepartmentIds!.Contains(deptInfo.SubdeptId.Value))
                                    continue;
                            }
                        }
                        else
                        {
                            if (isDeptFilterActive || isSubdeptFilterActive)
                                continue;
                        }

                        // 2. ПОЛУЧЕНИЕ ИМЕН ОТДЕЛА И ПОДОТДЕЛА (добавлено)
                        string deptName = "Без отдела";
                        string subdeptName = "Без подотдела";

                        if (!string.IsNullOrEmpty(itemKey) && storeItemsCache.TryGetValue(itemKey, out var infoForName))
                        {
                            if (infoForName.DeptId.HasValue && deptNames.TryGetValue(infoForName.DeptId.Value, out var dName))
                                deptName = dName;

                            if (infoForName.SubdeptId.HasValue && subdeptNames.TryGetValue(infoForName.SubdeptId.Value, out var sdName))
                                subdeptName = sdName;
                        }

                        var sheetName = string.IsNullOrWhiteSpace(sheet.Name) ? "Неизвестный лист" : sheet.Name;
                        var price = (decimal)sheet.Price;
                        var qty = sheet.Quantity;

                        // 3. КЛЮЧ ГРУППИРОВКИ (включает отделы, чтобы они не сливались в одну кучу)
                        var key = $"{deptName}_{subdeptName}_{sheetName}_{price}";

                        if (!sheetGroups.ContainsKey(key))
                        {
                            sheetGroups[key] = new SheetStatEntry
                            {
                                DepartmentName = deptName,
                                SubdepartmentName = subdeptName,
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

            return new StoreStatsResult
            {
                Entries = sheetGroups.Values.OrderByDescending(x => x.TotalSum).ToList(),
                TotalSheets = totalSheets,
                TotalCost = totalCost,
                UniqueTypes = sheetGroups.Count,
                AveragePricePerSheet = totalSheets > 0 ? totalCost / totalSheets : 0
            };
        }

        public async Task<DepartmentStatsResult> GetDepartmentStatsAsync(string userId, DateTime startDate, DateTime endDate,
            List<int>? selectedDepartmentIds = null, List<int>? selectedSubdepartmentIds = null)
        {
            var result = await GetSheetStatsAsync(userId, startDate, endDate, selectedDepartmentIds, selectedSubdepartmentIds);

            // Агрегируем подотделы по отделам (суммируем количество и сумму)
            var deptAggregated = result.DepartmentEntries
                .GroupBy(x => new { x.DepartmentId, x.DepartmentName })
                .Select(g => new DepartmentStatsEntry
                {
                    DepartmentId = g.Key.DepartmentId,
                    DepartmentName = g.Key.DepartmentName,
                    SubdepartmentId = null, // Для отдела это null
                    SubdepartmentName = "—",
                    TotalQuantity = g.Sum(x => x.TotalQuantity),
                    TotalSum = g.Sum(x => x.TotalSum)
                })
                .ToList();

            return new DepartmentStatsResult
            {
                TopDepartments = deptAggregated
                    .OrderByDescending(x => x.TotalSum)
                    .Take(5)
                    .ToList(),
                TopSubdepartments = result.DepartmentEntries
                    .Where(x => x.SubdepartmentId.HasValue)
                    .OrderByDescending(x => x.TotalSum)
                    .Take(5)
                    .ToList(),
                TotalCost = result.TotalCost,
                TotalSheets = result.TotalSheets
            };
        }


    }

    public class DepartmentStatsResult
    {
        public List<DepartmentStatsEntry> TopDepartments { get; set; } = new();
        public List<DepartmentStatsEntry> TopSubdepartments { get; set; } = new();
        public int TotalSheets { get; set; }
        public decimal TotalCost { get; set; }
    }

    // === DTO ===
    public class SheetStatEntry
    {
        public string DepartmentName { get; set; } = "Без отдела";
        public string SubdepartmentName { get; set; } = "Без подотдела";
        public string SheetName { get; set; } = "—";
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalSum => TotalQuantity * Price;
    }

    // Существующие DTO оставляем, добавляем новые:

    public class DepartmentStatsEntry
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "—";
        public int? SubdepartmentId { get; set; }
        public string SubdepartmentName { get; set; } = "—";
        public int TotalQuantity { get; set; }
        public decimal TotalSum { get; set; }
    }

    public class StoreStatsResult
    {
        public List<SheetStatEntry> Entries { get; set; } = new();
        public List<DepartmentStatsEntry> DepartmentEntries { get; set; } = new();
        public int TotalSheets { get; set; }
        public decimal TotalCost { get; set; }
        public int UniqueTypes { get; set; }
        public decimal AveragePricePerSheet { get; set; }

        // Для графиков
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartQtyData { get; set; } = new();
        public List<string> PieLabels { get; set; } = new();
        public List<decimal> PieCostData { get; set; } = new();
    }

}