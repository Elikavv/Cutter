using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Cutter.Data;

namespace Cutter.Services
{
    public interface ICashierService
    {
        Task<CashierDashboardData> GetDashboardDataAsync(DateTime startDate, DateTime endDate);
        Task<bool> UpdatePaidStatusAsync(int planId, bool isPaid);
    }

    public class CashierService : ICashierService
    {
        private readonly ApplicationDbContext _dbContext;

        public CashierService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CashierDashboardData> GetDashboardDataAsync(DateTime startDate, DateTime endDate)
        {
            var endDateEndOfDay = endDate.AddDays(1).AddTicks(-1);

            var plans = await _dbContext.CutPlan
                .Where(cp => cp.IsSave && cp.CreateDate >= startDate && cp.CreateDate <= endDateEndOfDay)
                .ToListAsync();

            var result = new CashierDashboardData();
            int totalProducts = 0;
            decimal totalProductAmount = 0;
            decimal totalCutAmount = 0;
            int paidCount = 0;
            int unpaidCount = 0;

            foreach (var p in plans)
            {
                var (products, productAmount, cutAmount) = CalculateStats(p.CuttingPlan);
                totalProducts += products;
                totalProductAmount += productAmount;
                totalCutAmount += cutAmount;

                var planTotal = productAmount + cutAmount;
                if (p.IsPaid)
                {
                    paidCount++;
                    result.Stats.PaidAmount += planTotal;
                    result.Stats.ProductPaidAmount += productAmount;
                    result.Stats.CutPaidAmount += cutAmount;
                }
                else
                {
                    unpaidCount++;
                    result.Stats.UnpaidAmount += planTotal;
                    result.Stats.ProductUnpaidAmount += productAmount;
                    result.Stats.CutUnpaidAmount += cutAmount;
                }

                result.Plans.Add(new CutPlanForCashier
                {
                    Id = p.Id,
                    GUUID = p.GUUID,
                    CutNum = p.NumCut,
                    CreateDate = p.CreateDate,
                    IsPaid = p.IsPaid,
                    Products = products,
                    ProductAmount = productAmount,
                    CutAmount = cutAmount
                });
            }

            // Группировка по дням (сортировка по убыванию)
            result.GroupedPlans = result.Plans
                .OrderByDescending(p => p.CreateDate)
                .GroupBy(p => p.CreateDate.Date)
                .Select(g => new DayGroup
                {
                    Date = g.Key,
                    Plans = g.ToList(),
                    DailyTotal = g.Sum(p => p.ProductAmount + p.CutAmount),
                    DailyPaid = g.Where(p => p.IsPaid).Sum(p => p.ProductAmount + p.CutAmount),
                    DailyUnpaid = g.Where(p => !p.IsPaid).Sum(p => p.ProductAmount + p.CutAmount),
                    PaidCount = g.Count(p => p.IsPaid),
                    UnpaidCount = g.Count(p => !p.IsPaid)
                }).ToList();

            // Подготовка данных для графиков (сортировка по возрастанию для оси X)
            var dailyData = result.GroupedPlans.OrderBy(g => g.Date).ToList();
            result.ChartLabels = dailyData.Select(d => d.Date.ToString("dd.MM")).ToList();
            result.ChartProductData = dailyData.Select(d => d.Plans.Sum(p => p.ProductAmount)).ToList();
            result.ChartCutData = dailyData.Select(d => d.Plans.Sum(p => p.CutAmount)).ToList();

            var totalPlans = plans.Count;
            result.Stats.TotalPlans = totalPlans;
            result.Stats.PaidPlans = paidCount;
            result.Stats.UnpaidPlans = unpaidCount;
            result.Stats.PaymentPercentage = totalPlans > 0 ? (paidCount * 100.0 / totalPlans) : 0;
            result.Stats.UnpaidPercentage = totalPlans > 0 ? (unpaidCount * 100.0 / totalPlans) : 0;
            result.Stats.TotalProducts = totalProducts;
            result.Stats.TotalProductAmount = totalProductAmount;
            result.Stats.TotalCutAmount = totalCutAmount;

            return result;
        }

        public async Task<bool> UpdatePaidStatusAsync(int planId, bool isPaid)
        {
            var plan = await _dbContext.CutPlan.FindAsync(planId);
            if (plan != null)
            {
                plan.IsPaid = isPaid;
                plan.ModifyDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        private (int products, decimal productAmount, decimal cutAmount) CalculateStats(string? json)
        {
            if (string.IsNullOrEmpty(json)) return (0, 0, 0);
            try
            {
                var plans = JsonConvert.DeserializeObject<List<CuttingPlan>>(json);
                if (plans == null) return (0, 0, 0);

                int totalProducts = 0;
                decimal totalProductAmount = 0;
                decimal totalCutAmount = 0;

                foreach (var plan in plans)
                {
                    foreach (var layout in plan.SheetLayouts)
                    {
                        totalProducts += 1;
                        totalProductAmount += (decimal)layout.Sheet.Price;

                        var cutCount = layout.CutLines?.Count() ?? 0;
                        totalCutAmount += (decimal)(cutCount * layout.Sheet.CutPrice);
                    }
                }
                return (totalProducts, totalProductAmount, totalCutAmount);
            }
            catch
            {
                return (0, 0, 0);
            }
        }
    }

    // === DTO (Модели передачи данных) ===
    public class CashierDashboardData
    {
        public List<CutPlanForCashier> Plans { get; set; } = new();
        public List<DayGroup> GroupedPlans { get; set; } = new();
        public CashierStats Stats { get; set; } = new();
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartProductData { get; set; } = new();
        public List<decimal> ChartCutData { get; set; } = new();
        public decimal PaidAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
    }

    public class DayGroup
    {
        public DateTime Date { get; set; }
        public List<CutPlanForCashier> Plans { get; set; } = new();
        public decimal DailyTotal { get; set; }
        public decimal DailyPaid { get; set; }
        public decimal DailyUnpaid { get; set; }
        public int PaidCount { get; set; }
        public int UnpaidCount { get; set; }
    }

    public class CutPlanForCashier
    {
        public int Id { get; set; }
        public string GUUID { get; set; } = string.Empty;
        public string CutNum { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public bool IsPaid { get; set; }
        public int Products { get; set; }
        public decimal ProductAmount { get; set; }
        public decimal CutAmount { get; set; }
    }

    public class CashierStats
    {
        public int TotalPlans { get; set; }
        public int PaidPlans { get; set; }
        public int UnpaidPlans { get; set; }
        public double PaymentPercentage { get; set; }
        public double UnpaidPercentage { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalProductAmount { get; set; }
        public decimal TotalCutAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
        // Разбивка по категориям
        public decimal ProductPaidAmount { get; set; }
        public decimal ProductUnpaidAmount { get; set; }
        public decimal CutPaidAmount { get; set; }
        public decimal CutUnpaidAmount { get; set; }
    }
}