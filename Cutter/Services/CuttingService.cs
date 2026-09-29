using Cutter.Components.Account.Pages.Manage;
using Cutter.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Numerics;


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


    // Параметры подрезки кромки
    [Flags]
    public enum SheetEdge
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Top = 1 << 3,
        All = Left | Right | Bottom | Top
    }

    public class TrimmingSettings
    {
        public SheetEdge Edges { get; set; } = SheetEdge.None;
        public double LeftMargin { get; set; } = 0;
        public double RightMargin { get; set; } = 0;
        public double BottomMargin { get; set; } = 0;
        public double TopMargin { get; set; } = 0;

        public bool Has(SheetEdge edge) => (Edges & edge) == edge;
    }

    // На главной странице пользователей
    public class UserCuttingSummaryDto
    {
        public string GUUID { get; set; } = string.Empty;
        public string NumCut { get; set; } = string.Empty;
        public string? Invoice { get; set; }
        public DateTime CreateDate { get; set; }
        public string? UserName { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalServicesAmount { get; set; }
        public decimal TotalSheetsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int ShiftNumber { get; set; }
    }

    public class UserDashboardDto
    {
        public int CuttingPlansCount { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalServicesAmount { get; set; }
        public decimal TotalSheetsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<UserCuttingSummaryDto> TodayCuttings { get; set; } = new();
    }


    // DTO для строки таблицы в админ-панели
    public class AdminCuttingPlanDto
    {
        public string GUUID { get; set; } = string.Empty;
        public string NumCut { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public bool IsSave { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalServicesAmount { get; set; }
        public decimal TotalSheetsAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // DTO для карточек статистики
    public class AdminDashboardSummaryDto
    {
        public int TotalPlans { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalServicesAmount { get; set; }
        public decimal TotalSheetsAmount { get; set; }
    }

    // Общий DTO для ответа сервиса
    public class AdminDashboardDto
    {
        public AdminDashboardSummaryDto Summary { get; set; } = new();
        public List<AdminCuttingPlanDto> Plans { get; set; } = new();
        public List<Store> AllStores { get; set; } = new();
        public List<ApplicationUser> AllUsers { get; set; } = new();
    }

    // Manager DTO
    public class StoreStatDto
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int TotalPlans { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalAmount { get; set; }
        public List<EmployeeStatDto> Employees { get; set; } = new();
    }

    public class EmployeeStatDto
    {
        public string UserName { get; set; } = string.Empty;
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int PlansCount { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DailyStatDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public int PlansCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ManagerDashboardDto
    {
        public int TotalPlans { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalAmount { get; set; }
        public EmployeeStatDto? TopEmployee { get; set; }
        public List<EmployeeStatDto> TopEmployees { get; set; } = new();
        public List<DailyStatDto> DailyStats { get; set; } = new();
        public List<ManagerCuttingPlanDto> RecentPlans { get; set; } = new();
        public List<StoreStatDto> StoreStats { get; set; } = new(); // НОВОЕ: статистика по магазинам
    }

    public class ManagerCuttingPlanDto
    {
        public string GUUID { get; set; } = string.Empty;
        public string NumCut { get; set; } = string.Empty;
        public int ShiftNumber { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty; // НОВОЕ
        public DateTime CreateDate { get; set; }
        public bool IsSave { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DailyTrendDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public int ActualPlans { get; set; } // Бланки
        public int ActualCuts { get; set; }
        public decimal ActualAmount { get; set; }
        public int? ForecastPlans { get; set; } // Прогноз бланков
        public int? ForecastCuts { get; set; } // Прогноз резов
        public decimal? ForecastAmount { get; set; } // Прогноз суммы
    }

    public class EmployeePerformanceDto
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalCuts { get; set; }
        public decimal TotalAmount { get; set; }
        public int WorkDays { get; set; }
        public decimal AvgPerDay => WorkDays > 0 ? TotalAmount / WorkDays : 0;
    }

    public class ManagerStatsDto
    {
        public int TotalPlans { get; set; }
        public int TotalCuts { get; set; }
        public decimal TotalAmount { get; set; } // Только услуги!
        public List<DailyTrendDto> TrendData { get; set; } = new();
        public List<EmployeePerformanceDto> EmployeeStats { get; set; } = new();
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

        public async Task<string?> GetUserStoreCodeAsync(string userId)
        {
            return await _db.Users
                .Where(x => x.Id == userId)
                .Select(s => s.Store.StoreCode)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetNumCutAsync(string hashId)
        {
            /*return await _db.Users
                .Where(x => x.Id == userId)
                .Select(s => s.Store.StoreCode)
                .FirstOrDefaultAsync();*/


            return await _db.CutPlan
                .Where(h => h.GUUID == hashId)
                .Select(n => n.NumCut)
                .FirstOrDefaultAsync();
        }

        public CuttingPlan OptimizeCutting(List<Sheet> availableSheets, List<Detail> details)
        {
            //return GuillotineAlgorithm3.GuillotineCut(availableSheets, details);
            return GuillotineAlgorithm4.GuillotineCut(availableSheets, details);
        }

        public async Task<string?> StoreCuttingPlan(List<CuttingPlan> plans, string UserId = null, string? Invoice = null, bool isSave = false)
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
                IsSave = isSave,
                Invoice = string.IsNullOrEmpty(Invoice) ? null : Invoice
            });

            await _db.SaveChangesAsync();

            return guid;

        }

        public async Task<UserCuttingPlan> GetCuttingPlanUser(string HashId)
        {
            if (string.IsNullOrEmpty(HashId)) return null;

            // 1. Сначала получаем саму запись с необходимыми связями
            var cp = await _db.CutPlan
                .Include(x => x.User).ThenInclude(u => u.Store)
                .FirstOrDefaultAsync(x => x.GUUID == HashId);

            if (cp == null) return null;

            // 2. Вычисляем номер в смене: считаем все бланки этого магазина за сегодня, 
            // которые были созданы раньше текущего (или имеют меньший Id, если время совпало)
            var shiftNumber = await _db.CutPlan.CountAsync(x =>
                x.User.StoreId == cp.User.StoreId &&
                x.CreateDate.Date == cp.CreateDate.Date &&
                (x.CreateDate < cp.CreateDate || (x.CreateDate == cp.CreateDate && x.Id <= cp.Id))
            );

            // 3. Формируем DTO (здесь уже безопасно использовать JsonConvert)
            return new UserCuttingPlan
            {
                StoreName = cp.User.Store.Name,
                UserName = cp.User.UserName,
                Phone = cp.User.Store.Phone,
                StoreAddress = cp.User.Store.Address,
                CutBarCode = cp.User.Store.CutBarCode,
                NumCut = cp.NumCut,
                ShiftNumber = shiftNumber, // <-- Заполняем вычисленный номер
                CuttingPlan = JsonConvert.DeserializeObject<List<CuttingPlan>>(cp.CuttingPlan),
                Invoice = cp.Invoice
            };
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

        /*public async Task SetCuttingPlanLocked(string hashId, bool isLocked)
        {
            var cutPlan = _db.CutPlan.FirstOrDefault(p => p.GUUID == hashId);
            if (cutPlan != null)
            {
                cutPlan.IsSave = isLocked;
                if (isLocked)
                    cutPlan.SaveDate = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsCuttingPlanLocked(string hashId)
        {
            var cutPlan = _db.CutPlan.FirstOrDefault(p => p.GUUID == hashId);
            return cutPlan?.IsSave ?? false;
        }*/

        public async Task UpdateCuttingPlan(string hashId, List<CuttingPlan> plans, string userId, string invoice)
        {
            var cutPlan = _db.CutPlan.FirstOrDefault(p => p.GUUID == hashId);
            if (cutPlan != null)
            {
                cutPlan.CuttingPlan = JsonConvert.SerializeObject(plans);
                cutPlan.ModifierId = userId;
                cutPlan.Invoice = invoice;
                cutPlan.ModifyDate = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }
        public async Task<DateTime?> GetCuttingPlanDate(string hashId)
        {
            var cutPlan = _db.CutPlan.FirstOrDefault(p => p.GUUID == hashId);
            return cutPlan?.CreateDate;
        }

        public async Task<UserDashboardDto> GetUserDashboardAsync(string userId, bool isAdmin)
        {
            var today = DateTime.Today;

            // 1. Получаем данные, сортируя по возрастанию (чтобы правильно посчитать порядковый номер)
            var query = _db.CutPlan
                .Include(cp => cp.User)
                .Where(cp => cp.CreateDate.Date == today && cp.IsSave && (isAdmin || cp.UserId == userId));

            var dbPlans = await query.OrderBy(cp => cp.CreateDate).ThenBy(cp => cp.Id).ToListAsync();

            var result = new UserDashboardDto
            {
                CuttingPlansCount = dbPlans.Count,
                TodayCuttings = new List<UserCuttingSummaryDto>()
            };

            // 2. Словарь-счетчик: для каждого магазина свой счетчик, начинающийся с 1
            var storeCounters = new Dictionary<Guid, int>();

            foreach (var dbPlan in dbPlans)
            {
                var storeId = dbPlan.User?.StoreId ?? Guid.Empty;
                if (!storeCounters.ContainsKey(storeId))
                {
                    storeCounters[storeId] = 0;
                }
                storeCounters[storeId]++;
                int shiftNumber = storeCounters[storeId]; // Это и есть номер бланка в смене для этого магазина

                var plans = JsonConvert.DeserializeObject<List<CuttingPlan>>(dbPlan.CuttingPlan);

                int planCuts = 0;
                decimal planServices = 0;
                decimal planSheets = 0;

                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        foreach (var layout in plan.SheetLayouts)
                        {
                            planCuts += layout.CutCount;

                            if (layout.Services != null && layout.Services.Any())
                            {
                                planServices += layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                            }
                            else
                            {
                                planServices += (decimal)(layout.CutCount * (layout.Sheet?.CutPrice ?? 0));
                            }

                            if (layout.Sheet != null && layout.Sheet.SKU != "OWN")
                            {
                                planSheets += (decimal)(layout.Sheet.Price * layout.Sheet.Quantity);
                            }
                        }
                    }
                }

                result.TotalCuts += planCuts;
                result.TotalServicesAmount += planServices;
                result.TotalSheetsAmount += planSheets;
                result.TotalAmount += (planServices + planSheets);

                result.TodayCuttings.Add(new UserCuttingSummaryDto
                {
                    GUUID = dbPlan.GUUID,
                    NumCut = dbPlan.NumCut,
                    ShiftNumber = shiftNumber, // <--- ЗАПОЛНЯЕМ НОМЕР В СМЕНЕ
                    Invoice = dbPlan.Invoice,
                    CreateDate = dbPlan.CreateDate,
                    UserName = dbPlan.User?.UserName ?? "Неизвестно",
                    TotalCuts = planCuts,
                    TotalServicesAmount = planServices,
                    TotalSheetsAmount = planSheets,
                    TotalAmount = planServices + planSheets
                });
            }

            // 3. Сортируем обратно по убыванию даты, чтобы в интерфейсе новые были сверху
            result.TodayCuttings = result.TodayCuttings.OrderByDescending(x => x.CreateDate).ToList();

            return result;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync(
            DateTime startDate,
            DateTime endDate,
            IEnumerable<Guid> storeIds,
            IEnumerable<string> userIds,
            bool showSaved,
            bool showDrafts)
        {
            var result = new AdminDashboardDto();

            // 1. Загружаем справочники (они маленькие, это быстро)
            result.AllStores = await _db.Stores.OrderBy(s => s.Name).ToListAsync();
            result.AllUsers = await _db.Users.Include(u => u.Store).OrderBy(u => u.UserName).ToListAsync();

            // 2. Формируем запрос к БД
            var query = _db.CutPlan
                .Include(cp => cp.User).ThenInclude(u => u.Store)
                .Where(cp => cp.CreateDate.Date >= startDate && cp.CreateDate.Date <= endDate);

            // Применяем фильтры НА СТОРОНЕ БД (это критически важно для производительности)
            if (storeIds != null && storeIds.Any())
            {
                query = query.Where(cp => storeIds.Contains(cp.User.StoreId));
            }

            if (userIds != null && userIds.Any())
            {
                query = query.Where(cp => userIds.Contains(cp.UserId));
            }

            query = query.Where(cp => (cp.IsSave && showSaved) || (!cp.IsSave && showDrafts));

            // 3. Выполняем запрос
            var dbPlans = await query.OrderByDescending(cp => cp.CreateDate).ToListAsync();

            // 4. Обрабатываем результаты и считаем статистику
            foreach (var dbPlan in dbPlans)
            {
                var plans = JsonConvert.DeserializeObject<List<CuttingPlan>>(dbPlan.CuttingPlan);

                int planCuts = 0;
                decimal planServices = 0;
                decimal planSheets = 0;

                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        foreach (var layout in plan.SheetLayouts)
                        {
                            // Правильный расчет: берем CutCount
                            planCuts += layout.CutCount;

                            // Правильный расчет: суммируем услуги
                            if (layout.Services != null && layout.Services.Any())
                            {
                                planServices += layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                            }
                            else
                            {
                                planServices += (decimal)(layout.CutCount * (layout.Sheet?.CutPrice ?? 0));
                            }

                            // Правильный расчет: суммируем листы (исключая "Свой материал")
                            if (layout.Sheet != null && layout.Sheet.SKU != "OWN")
                            {
                                planSheets += (decimal)(layout.Sheet.Price * layout.Sheet.Quantity);
                            }
                        }
                    }
                }

                result.Summary.TotalPlans++;
                result.Summary.TotalCuts += planCuts;
                result.Summary.TotalServicesAmount += planServices;
                result.Summary.TotalSheetsAmount += planSheets;

                result.Plans.Add(new AdminCuttingPlanDto
                {
                    GUUID = dbPlan.GUUID,
                    NumCut = dbPlan.NumCut,
                    StoreName = dbPlan.User?.Store?.Name ?? "—",
                    UserName = dbPlan.User?.UserName ?? "—",
                    CreateDate = dbPlan.CreateDate,
                    IsSave = dbPlan.IsSave,
                    TotalCuts = planCuts,
                    TotalServicesAmount = planServices,
                    TotalSheetsAmount = planSheets,
                    TotalAmount = planServices + planSheets
                });
            }

            return result;
        }


        public async Task<ManagerDashboardDto> GetManagerDashboardAsync(string managerId, DateTime startDate, DateTime endDate)
        {
            var result = new ManagerDashboardDto();

            // 1. Находим все магазины менеджера через таблицу ManagerStores
            var managedStoreIds = await _db.ManagerStores
                .Where(ms => ms.ManagerId == managerId)
                .Select(ms => ms.StoreId)
                .ToListAsync();

            // Fallback: если нет записей в ManagerStores, пробуем найти по StoreId пользователя
            if (!managedStoreIds.Any())
            {
                var manager = await _db.Users.FindAsync(managerId);
                if (manager?.StoreId != null)
                {
                    managedStoreIds.Add(manager.StoreId);
                }
            }

            if (!managedStoreIds.Any()) return result;

            // 2. Находим всех подчиненных (роль "User") во всех магазинах менеджера
            var subordinates = await _db.Users
                .Include(u => u.Store)
                .Where(u => managedStoreIds.Contains(u.StoreId) && (u.Role == "User" ||u.Role == "Manager" ))
                .OrderBy(u => u.Store.Name)
                .ThenBy(u => u.UserName)
                .ToListAsync();

            var subordinateIds = subordinates.Select(u => u.Id).ToList();
            if (!subordinateIds.Any()) return result;

            // 3. Загружаем все планы за период
            var dbPlans = await _db.CutPlan
                .Include(cp => cp.User).ThenInclude(u => u.Store)
                .Where(cp => cp.CreateDate.Date >= startDate && cp.CreateDate.Date <= endDate)
                .Where(cp => subordinateIds.Contains(cp.UserId))
                .OrderByDescending(cp => cp.CreateDate)
                .ToListAsync();

            // 4. Определяем формат группировки: по часам или по дням
            var uniqueDates = dbPlans.Select(p => p.CreateDate.Date).Distinct().ToList();
            bool groupByHours = uniqueDates.Count == 1;

            // 5. Считаем статистику
            var userStats = new Dictionary<string, EmployeeStatDto>();
            var dailyStats = new Dictionary<string, DailyStatDto>();
            var storeStats = new Dictionary<Guid, StoreStatDto>();

            foreach (var dbPlan in dbPlans)
            {
                decimal planAmount = 0;
                int planCuts = 0;

                var plans = JsonConvert.DeserializeObject<List<CuttingPlan>>(dbPlan.CuttingPlan);
                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        foreach (var layout in plan.SheetLayouts)
                        {
                            planCuts += layout.CutCount;

                            // Считаем ТОЛЬКО услуги (без стоимости листов)
                            if (layout.Services != null && layout.Services.Any())
                                planAmount += layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                            else
                                planAmount += (decimal)(layout.CutCount * (layout.Sheet?.CutPrice ?? 0));
                        }
                    }
                }

                result.TotalPlans++;
                result.TotalCuts += planCuts;
                result.TotalAmount += planAmount;

                // Статистика по сотрудникам
                var uName = dbPlan.User?.UserName ?? "Неизвестно";
                var storeName = dbPlan.User?.Store?.Name ?? "Неизвестный магазин";
                var storeId = dbPlan.User?.StoreId ?? Guid.Empty;

                if (!userStats.ContainsKey(uName))
                    userStats[uName] = new EmployeeStatDto
                    {
                        UserName = uName,
                        StoreId = storeId,
                        StoreName = storeName
                    };

                userStats[uName].PlansCount++;
                userStats[uName].TotalCuts += planCuts;
                userStats[uName].TotalAmount += planAmount;

                // Статистика по магазинам
                if (!storeStats.ContainsKey(storeId))
                    storeStats[storeId] = new StoreStatDto
                    {
                        StoreId = storeId,
                        StoreName = storeName
                    };

                storeStats[storeId].TotalPlans++;
                storeStats[storeId].TotalCuts += planCuts;
                storeStats[storeId].TotalAmount += planAmount;

                // Добавляем сотрудника в статистику магазина
                if (!storeStats[storeId].Employees.Any(e => e.UserName == uName))
                {
                    storeStats[storeId].Employees.Add(new EmployeeStatDto
                    {
                        UserName = uName,
                        StoreId = storeId,
                        StoreName = storeName
                    });
                }
                var empInStore = storeStats[storeId].Employees.First(e => e.UserName == uName);
                empInStore.PlansCount++;
                empInStore.TotalCuts += planCuts;
                empInStore.TotalAmount += planAmount;

                // Статистика по времени (по часам или по дням)
                var timeLabel = groupByHours
                    ? dbPlan.CreateDate.ToString("HH:mm")
                    : dbPlan.CreateDate.ToString("dd.MM");

                if (!dailyStats.ContainsKey(timeLabel))
                    dailyStats[timeLabel] = new DailyStatDto { DateLabel = timeLabel };

                dailyStats[timeLabel].PlansCount++;
                dailyStats[timeLabel].TotalAmount += planAmount;
            }

            // 6. Формируем итоговые списки
            result.TopEmployees = userStats.Values.OrderByDescending(x => x.TotalAmount).ToList();
            result.TopEmployee = result.TopEmployees.FirstOrDefault();

            // Сортировка зависит от формата
            result.DailyStats = groupByHours
                ? dailyStats.Values.OrderBy(x => x.DateLabel).ToList()
                : dailyStats.Values.OrderBy(x => DateTime.ParseExact(x.DateLabel, "dd.MM", null)).ToList();

            result.StoreStats = storeStats.Values.OrderBy(s => s.StoreName).ToList();

            // 7. Последние бланки (до 50 штук) с правильной суммой
            result.RecentPlans = dbPlans.Take(50).Select(cp =>
            {
                decimal planAmount = 0;
                var plans = JsonConvert.DeserializeObject<List<CuttingPlan>>(cp.CuttingPlan);
                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        foreach (var layout in plan.SheetLayouts)
                        {
                            // Только услуги
                            if (layout.Services != null && layout.Services.Any())
                                planAmount += layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                            else
                                planAmount += (decimal)(layout.CutCount * (layout.Sheet?.CutPrice ?? 0));
                        }
                    }
                }

                return new ManagerCuttingPlanDto
                {
                    GUUID = cp.GUUID,
                    NumCut = cp.NumCut,
                    UserName = cp.User?.UserName ?? "—",
                    StoreName = cp.User?.Store?.Name ?? "—",
                    CreateDate = cp.CreateDate,
                    IsSave = cp.IsSave,
                    TotalAmount = planAmount
                };
            }).ToList();

            return result;
        }


        public async Task<ManagerStatsDto> GetManagerStatsAsync(string managerId, DateTime startDate, DateTime endDate, string? filterUserId = null)
        {
            var result = new ManagerStatsDto();

            // 1. Магазины менеджера
            var managedStoreIds = await _db.ManagerStores.Where(ms => ms.ManagerId == managerId).Select(ms => ms.StoreId).ToListAsync();
            if (!managedStoreIds.Any())
            {
                var manager = await _db.Users.FindAsync(managerId);
                if (manager?.StoreId != null) managedStoreIds.Add(manager.StoreId);
            }
            if (!managedStoreIds.Any()) return result;

            // 2. Подчиненные
            var queryUsers = _db.Users.Where(u => managedStoreIds.Contains(u.StoreId) && (u.Role == "User" || u.Role == "Manager"));
            if (!string.IsNullOrEmpty(filterUserId))
            {
                queryUsers = queryUsers.Where(u => u.Id == filterUserId);
            }
            var subordinateIds = await queryUsers.Select(u => u.Id).ToListAsync();
            if (!subordinateIds.Any()) return result;

            // 3. Данные за период
            var dbPlans = await _db.CutPlan
                .Include(cp => cp.User)
                .Where(cp => cp.CreateDate.Date >= startDate && cp.CreateDate.Date <= endDate)
                .Where(cp => subordinateIds.Contains(cp.UserId))
                .OrderBy(cp => cp.CreateDate)
                .ToListAsync();

            var dailyData = new Dictionary<string, DailyTrendDto>();
            var empData = new Dictionary<string, EmployeePerformanceDto>();
            var empWorkDays = new Dictionary<string, HashSet<string>>();

            foreach (var cp in dbPlans)
            {
                int cuts = 0;
                decimal amount = 0;
                var plans = JsonConvert.DeserializeObject<List<CuttingPlan>>(cp.CuttingPlan);

                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        foreach (var layout in plan.SheetLayouts)
                        {
                            cuts += layout.CutCount;
                            // ВАЖНО: Считаем ТОЛЬКО услуги, без стоимости листов
                            if (layout.Services != null && layout.Services.Any())
                                amount += layout.Services.Sum(s => (decimal)s.Price * s.Quantity);
                            else
                                amount += (decimal)(layout.CutCount * (layout.Sheet?.CutPrice ?? 0));
                        }
                    }
                }

                result.TotalPlans++;
                result.TotalCuts += cuts;
                result.TotalAmount += amount;

                // Группировка по дням
                var dateLabel = cp.CreateDate.ToString("dd.MM");
                if (!dailyData.ContainsKey(dateLabel))
                    dailyData[dateLabel] = new DailyTrendDto { DateLabel = dateLabel };
                dailyData[dateLabel].ActualPlans++; // ✅ Добавлено
                dailyData[dateLabel].ActualCuts += cuts;
                dailyData[dateLabel].ActualAmount += amount;

                // Группировка по сотрудникам
                var uName = cp.User?.UserName ?? "Неизвестно";
                if (!empData.ContainsKey(uName))
                    empData[uName] = new EmployeePerformanceDto { UserName = uName };

                empData[uName].TotalCuts += cuts;
                empData[uName].TotalAmount += amount;

                if (!empWorkDays.ContainsKey(uName))
                    empWorkDays[uName] = new HashSet<string>();
                empWorkDays[uName].Add(cp.CreateDate.ToString("yyyy-MM-dd"));
            }

            // 4. Расчет рабочих дней и сортировка сотрудников
            foreach (var kvp in empData)
            {
                kvp.Value.WorkDays = empWorkDays.ContainsKey(kvp.Key) ? empWorkDays[kvp.Key].Count : 0;
            }
            result.EmployeeStats = empData.Values.OrderByDescending(e => e.TotalAmount).ToList();

            // 5. Расчет простого прогноза (скользящее среднее за 3 дня)
            // 5. Расчет прогноза (скользящее среднее за 3 дня) по всем трем метрикам
            var sortedDays = dailyData.Values.OrderBy(d => DateTime.ParseExact(d.DateLabel, "dd.MM", null)).ToList();

            for (int i = 0; i < sortedDays.Count; i++)
            {
                result.TrendData.Add(sortedDays[i]);
            }

            // Добавляем точку прогноза на "завтра" по всем трем метрикам
            if (sortedDays.Count >= 3)
            {
                var last3 = sortedDays.TakeLast(3).ToList();

                int avgPlans = (last3[0].ActualPlans + last3[1].ActualPlans + last3[2].ActualPlans) / 3;
                int avgCuts = (last3[0].ActualCuts + last3[1].ActualCuts + last3[2].ActualCuts) / 3;
                decimal avgAmount = (last3[0].ActualAmount + last3[1].ActualAmount + last3[2].ActualAmount) / 3;

                var nextDate = DateTime.ParseExact(sortedDays.Last().DateLabel, "dd.MM", null).AddDays(1).ToString("dd.MM");

                result.TrendData.Add(new DailyTrendDto
                {
                    DateLabel = nextDate + " (прогноз)",
                    ActualPlans = 0,
                    ActualCuts = 0,
                    ActualAmount = 0,
                    ForecastPlans = avgPlans,
                    ForecastCuts = avgCuts,
                    ForecastAmount = avgAmount
                });
            }

            return result;
        }

        /// <summary>
        /// Возвращает номер бланка в смене. 
        /// Для новых бланков считает за сегодня. Для старых — за день их создания.
        /// </summary>
        public async Task<int> GetShiftNumberAsync(string userId, string? guuid = null)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user?.StoreId == null) return 1;

            // СЦЕНАРИЙ 1: Новый бланк (считаем только за СЕГОДНЯ)
            if (string.IsNullOrEmpty(guuid))
            {
                var today = DateTime.Today;
                return await _db.CutPlan.CountAsync(cp =>
                    cp.User.StoreId == user.StoreId &&
                    cp.CreateDate.Date == today) + 1;
            }

            // СЦЕНАРИЙ 2: Существующий бланк (может быть любым днем)
            var current = await _db.CutPlan.FirstOrDefaultAsync(cp => cp.GUUID == guuid);

            if (current == null) return 1; // Защита на случай, если бланк не найден

            // Берем дату ИМЕННО этого бланка, а не сегодня!
            var planDate = current.CreateDate.Date;

            // Считаем, сколько бланков в этом магазине в ЭТОТ день были созданы раньше 
            // (или имеют меньший/равный Id, если время создания совпало с точностью до миллисекунды)
            return await _db.CutPlan.CountAsync(cp =>
                cp.User.StoreId == user.StoreId &&
                cp.CreateDate.Date == planDate && // <-- ИСПРАВЛЕНО: используем дату самого бланка
                (cp.CreateDate < current.CreateDate || (cp.CreateDate == current.CreateDate && cp.Id <= current.Id)));
        }

        /// <summary>
        /// Возвращает номер СЛЕДУЮЩЕГО бланка (для нового, ещё не сохранённого)
        /// </summary>
        public async Task<int> GetNextShiftNumberAsync(string userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user?.StoreId == null) return 1;

            var today = DateTime.Today;
            var count = await _db.CutPlan
                .CountAsync(cp => cp.User.StoreId == user.StoreId && cp.CreateDate.Date == today);

            return count + 1;
        }

        /// <summary>
        /// Возвращает словарь GUUID -> номер в смене для списка бланков
        /// Используется на дашбордах, чтобы не делать N+1 запросов
        /// </summary>
        public async Task<Dictionary<string, int>> GetShiftNumbersMapAsync(DateTime date, IEnumerable<Guid> storeIds)
        {
            var plans = await _db.CutPlan
                .Include(cp => cp.User)
                .Where(cp => cp.CreateDate.Date == date && storeIds.Contains(cp.User.StoreId))
                .OrderBy(cp => cp.CreateDate)
                .ThenBy(cp => cp.Id)
                .ToListAsync();

            var result = new Dictionary<string, int>();
            var counters = new Dictionary<Guid, int>();

            foreach (var plan in plans)
            {
                var storeId = plan.User?.StoreId ?? Guid.Empty;
                if (!counters.ContainsKey(storeId))
                    counters[storeId] = 0;

                counters[storeId]++;
                result[plan.GUUID] = counters[storeId];
            }

            return result;
        }

        /// <summary>
        /// Получить ID магазинов, которыми управляет менеджер
        /// </summary>
        public async Task<List<Guid>> GetManagerStoreIdsAsync(string managerId)
        {
            var storeIds = await _db.ManagerStores
                .Where(ms => ms.ManagerId == managerId)
                .Select(ms => ms.StoreId)
                .ToListAsync();

            // Fallback: если нет записей в ManagerStores
            if (!storeIds.Any())
            {
                var manager = await _db.Users.FindAsync(managerId);
                if (manager?.StoreId != null)
                {
                    storeIds.Add(manager.StoreId);
                }
            }

            return storeIds;
        }

        /// <summary>
        /// Получить список подчиненных (распиловщиков) менеджера
        /// </summary>
        public async Task<List<ApplicationUser>> GetSubordinatesAsync(string managerId)
        {
            var storeIds = await GetManagerStoreIdsAsync(managerId);

            if (!storeIds.Any()) return new List<ApplicationUser>();

            return await _db.Users
                .Where(u => storeIds.Contains(u.StoreId) && (u.Role == "User" || u.Role == "Manager"))
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

    }

    /***********GuilotineCut************/
    /*public class GuillotineAlgorithm
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
                }
            }

            layout.CutCount = layout.CutLines.Count;
        }



    }*/

    /*public class GuillotineAlgorithm2
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
    }*/

    /*public class GuillotineAlgorithm3
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

    }*/



    public static class GuillotineAlgorithm4
    {
        private enum FreeRectChoiceHeuristic
        {
            BestAreaFit,
            BestShortSideFit,
            BestLongSideFit
        }

        private enum GuillotineSplitHeuristic
        {
            ShorterLeftoverAxis,
            LongerLeftoverAxis,
            MaximizeArea
        }

        private sealed class Rect
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }

            public Rect(double x, double y, double width, double height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        public static CuttingPlan GuillotineCut(
            List<Sheet> sheets,
            List<Detail> details,
            TrimmingSettings? trimming = null)
        {
            trimming ??= new TrimmingSettings();

            var plan = new CuttingPlan
            {
                OptimizationAlgorithm = "N-Stage Guillotine BSP with Collinear Merging"
            };

            var remainingDetails = ExpandDetails(details);
            int sheetNumber = 1;

            foreach (var sheetTemplate in sheets)
            {
                for (int q = 0; q < sheetTemplate.Quantity; q++)
                {
                    if (!remainingDetails.Any()) break;

                    var sheetInstance = sheetTemplate.Clone();
                    var layout = OptimizeSingleSheetTournament(sheetInstance, remainingDetails, sheetNumber, trimming);

                    if (layout.Details.Any())
                    {
                        layout.CutCount = layout.CutLines.Count;
                        layout.DetailsCount = layout.Details.Count;
                        layout.IsActive = true;

                        plan.SheetLayouts.Add(layout);
                        plan.TotalCuts += layout.CutCount;

                        var placedIds = layout.Details.Select(d => d.DetailId).ToHashSet();
                        remainingDetails = remainingDetails.Where(d => !placedIds.Contains(d.Id)).ToList();
                        sheetNumber++;
                    }
                }
            }

            plan.UnplacedDetails = remainingDetails;
            CalculateTotals(plan);

            return plan;
        }

        private static SheetLayout OptimizeSingleSheetTournament(
            Sheet sheet,
            List<Detail> details,
            int sheetNumber,
            TrimmingSettings trimming)
        {
            double trimL = Math.Max(sheet.MarginLeft, Math.Max(sheet.MinMargin, trimming?.LeftMargin ?? 0));
            double trimR = Math.Max(sheet.MarginRight, Math.Max(sheet.MinMargin, trimming?.RightMargin ?? 0));
            double trimB = Math.Max(sheet.MarginBottom, Math.Max(sheet.MinMargin, trimming?.BottomMargin ?? 0));
            double trimT = Math.Max(sheet.MarginTop, Math.Max(sheet.MinMargin, trimming?.TopMargin ?? 0));

            double usableWidth = sheet.Width - trimL - trimR;
            double usableLength = sheet.Length - trimB - trimT;

            var emptyLayout = new SheetLayout { Sheet = sheet };
            if (usableWidth <= 0 || usableLength <= 0) return emptyLayout;

            var trimCutLines = GenerateTrimmingCuts(sheet, trimming, trimL, trimR, trimB, trimT);



            // Набор турнирных стратегий сортировки деталей
            var sortStrategies = new Func<List<Detail>, List<Detail>>[]
            {
            d => d.OrderByDescending(x => x.Width * x.Length).ThenByDescending(x => Math.Max(x.Width, x.Length)).ToList(),
            d => d.OrderByDescending(x => Math.Max(x.Width, x.Length)).ThenByDescending(x => Math.Min(x.Width, x.Length)).ToList(),
            d => d.OrderByDescending(x => Math.Min(x.Width, x.Length)).ThenByDescending(x => x.Width * x.Length).ToList(),
            d => d.OrderByDescending(x => Math.Max(x.Width, x.Length) / Math.Max(0.01, Math.Min(x.Width, x.Length))).ToList()
            };

            var rectChoices = new[]
            {
            FreeRectChoiceHeuristic.BestShortSideFit,
            FreeRectChoiceHeuristic.BestAreaFit,
            FreeRectChoiceHeuristic.BestLongSideFit
        };

            var splitRules = new[]
            {
            GuillotineSplitHeuristic.LongerLeftoverAxis,
            GuillotineSplitHeuristic.ShorterLeftoverAxis,
            GuillotineSplitHeuristic.MaximizeArea
        };

            SheetLayout? bestLayout = null;
            double bestScore = -1.0;

            foreach (var sorter in sortStrategies)
            {
                var sortedDetails = sorter(details);

                foreach (var rectChoice in rectChoices)
                {
                    foreach (var splitRule in splitRules)
                    {
                        var testLayout = RunGuillotineSimulation(
                            sheet, sortedDetails, sheetNumber,
                            trimL, trimR, trimB, trimT,
                            usableWidth, usableLength,
                            rectChoice, splitRule, trimCutLines);

                        // Оценка: приоритет площади раскроя, штраф за количество резов
                        double usedArea = testLayout.Details.Sum(d => d.Width * d.Length);
                        double score = usedArea * 10000.0 - (testLayout.CutLines.Count * 2.0);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLayout = testLayout;
                        }
                    }
                }
            }

            if (bestLayout == null) return emptyLayout;

            // Постобработка: объединение коллинеарных линий резов
            var mergedCuts = MergeCollinearCuts(bestLayout.CutLines, sheet.BladeWidth);
            bestLayout.CutLines = mergedCuts;
            bestLayout.CutCount = mergedCuts.Count;

            return bestLayout;
        }

        private static SheetLayout RunGuillotineSimulation(
            Sheet sheet,
            List<Detail> sortedDetails,
            int sheetNumber,
            double trimL, double trimR, double trimB, double trimT,
            double usableWidth, double usableLength,
            FreeRectChoiceHeuristic rectChoice,
            GuillotineSplitHeuristic splitRule,
            List<CutLine> trimCutLines)
        {
            var layout = new SheetLayout { Sheet = sheet };
            var freeRects = new List<Rect> { new Rect(trimL, trimB, usableWidth, usableLength) };
            var cuts = new List<CutLine>(trimCutLines);
            int currentStage = 1;

            foreach (var detail in sortedDetails)
            {
                int bestRectIndex = -1;
                double bestScore1 = double.MaxValue;
                double bestScore2 = double.MaxValue;
                bool bestRotated = false;
                double placedW = 0;
                double placedH = 0;

                for (int i = 0; i < freeRects.Count; i++)
                {
                    var r = freeRects[i];

                    // Вариант без поворота
                    if (detail.Width <= r.Width && detail.Length <= r.Height)
                    {
                        ScorePlacement(r, detail.Width, detail.Length, rectChoice, out double s1, out double s2);
                        if (s1 < bestScore1 || (Math.Abs(s1 - bestScore1) < 0.001 && s2 < bestScore2))
                        {
                            bestScore1 = s1;
                            bestScore2 = s2;
                            bestRectIndex = i;
                            bestRotated = false;
                            placedW = detail.Width;
                            placedH = detail.Length;
                        }
                    }

                    // Вариант с поворотом
                    if (sheet.CanRotateParts && detail.CanRotate && detail.Length <= r.Width && detail.Width <= r.Height)
                    {
                        ScorePlacement(r, detail.Length, detail.Width, rectChoice, out double s1, out double s2);
                        if (s1 < bestScore1 || (Math.Abs(s1 - bestScore1) < 0.001 && s2 < bestScore2))
                        {
                            bestScore1 = s1;
                            bestScore2 = s2;
                            bestRectIndex = i;
                            bestRotated = true;
                            placedW = detail.Length;
                            placedH = detail.Width;
                        }
                    }
                }

                if (bestRectIndex < 0) continue;

                var targetRect = freeRects[bestRectIndex];
                freeRects.RemoveAt(bestRectIndex);

                layout.Details.Add(new CutDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    DetailId = detail.Id,
                    Name = detail.Name,
                    Width = Math.Round(placedW, 2),
                    Length = Math.Round(placedH, 2),
                    X = Math.Round(targetRect.X, 2),
                    Y = Math.Round(targetRect.Y, 2),
                    Rotated = bestRotated,
                    SheetId = sheet.Id,
                    SheetNumber = sheetNumber
                });

                SplitFreeRectGuillotine(
                    targetRect, placedW, placedH, sheet.BladeWidth,
                    splitRule, freeRects, cuts, currentStage++,
                    sheet.Width, sheet.Length);
            }

            layout.CutLines = cuts;
            double sheetArea = sheet.Width * sheet.Length;
            double totalUsedArea = layout.Details.Sum(d => d.Width * d.Length);
            layout.MaterialUsage = sheetArea > 0 ? (totalUsedArea / sheetArea) : 0;
            layout.WastePercentage = 100.0 - (layout.MaterialUsage * 100.0);

            return layout;
        }

        private static void ScorePlacement(
            Rect r, double w, double h,
            FreeRectChoiceHeuristic heuristic,
            out double score1, out double score2)
        {
            double remW = r.Width - w;
            double remH = r.Height - h;

            switch (heuristic)
            {
                case FreeRectChoiceHeuristic.BestAreaFit:
                    score1 = r.Width * r.Height - w * h;
                    score2 = Math.Min(remW, remH);
                    break;
                case FreeRectChoiceHeuristic.BestShortSideFit:
                    score1 = Math.Min(remW, remH);
                    score2 = Math.Max(remW, remH);
                    break;
                case FreeRectChoiceHeuristic.BestLongSideFit:
                    score1 = Math.Max(remW, remH);
                    score2 = Math.Min(remW, remH);
                    break;
                default:
                    score1 = remW * remH;
                    score2 = 0;
                    break;
            }
        }

        private static void SplitFreeRectGuillotine(
            Rect target, double placedW, double placedH, double blade,
            GuillotineSplitHeuristic splitRule,
            List<Rect> freeRects, List<CutLine> cuts, int stage,
            double sheetWidth, double sheetLength)
        {
            double remW = target.Width - placedW;
            double remH = target.Height - placedH;
            const double eps = 0.05;

            bool splitHorizontal = splitRule switch
            {
                GuillotineSplitHeuristic.ShorterLeftoverAxis => remW <= remH,
                GuillotineSplitHeuristic.LongerLeftoverAxis => remW > remH,
                GuillotineSplitHeuristic.MaximizeArea => (target.Width * remH) >= (remW * target.Height),
                _ => remW <= remH
            };

            if (splitHorizontal)
            {
                if (remH > eps && (target.Y + placedH < sheetLength - eps))
                {
                    cuts.Add(new CutLine
                    {
                        X1 = Math.Round(target.X, 2),
                        Y1 = Math.Round(target.Y + placedH, 2),
                        X2 = Math.Round(target.X + target.Width, 2),
                        Y2 = Math.Round(target.Y + placedH, 2),
                        Type = "horizontal",
                        Stage = stage,
                        IsVisible = true,
                        IsCut = false
                    });

                    double topY = target.Y + placedH + blade;
                    double topH = target.Height - placedH - blade;
                    if (topH > eps)
                    {
                        freeRects.Add(new Rect(target.X, topY, target.Width, topH));
                    }
                }

                if (remW > eps && (target.X + placedW < sheetWidth - eps))
                {
                    cuts.Add(new CutLine
                    {
                        X1 = Math.Round(target.X + placedW, 2),
                        Y1 = Math.Round(target.Y, 2),
                        X2 = Math.Round(target.X + placedW, 2),
                        Y2 = Math.Round(target.Y + placedH, 2),
                        Type = "vertical",
                        Stage = stage + 1,
                        IsVisible = true,
                        IsCut = false
                    });

                    double rightX = target.X + placedW + blade;
                    double rightW = target.Width - placedW - blade;
                    if (rightW > eps)
                    {
                        freeRects.Add(new Rect(rightX, target.Y, rightW, placedH));
                    }
                }
            }
            else
            {
                if (remW > eps && (target.X + placedW < sheetWidth - eps))
                {
                    cuts.Add(new CutLine
                    {
                        X1 = Math.Round(target.X + placedW, 2),
                        Y1 = Math.Round(target.Y, 2),
                        X2 = Math.Round(target.X + placedW, 2),
                        Y2 = Math.Round(target.Y + target.Height, 2),
                        Type = "vertical",
                        Stage = stage,
                        IsVisible = true,
                        IsCut = false
                    });

                    double rightX = target.X + placedW + blade;
                    double rightW = target.Width - placedW - blade;
                    if (rightW > eps)
                    {
                        freeRects.Add(new Rect(rightX, target.Y, rightW, target.Height));
                    }
                }

                if (remH > eps && (target.Y + placedH < sheetLength - eps))
                {
                    cuts.Add(new CutLine
                    {
                        X1 = Math.Round(target.X, 2),
                        Y1 = Math.Round(target.Y + placedH, 2),
                        X2 = Math.Round(target.X + placedW, 2),
                        Y2 = Math.Round(target.Y + placedH, 2),
                        Type = "horizontal",
                        Stage = stage + 1,
                        IsVisible = true,
                        IsCut = false
                    });

                    double topY = target.Y + placedH + blade;
                    double topH = target.Height - placedH - blade;
                    if (topH > eps)
                    {
                        freeRects.Add(new Rect(target.X, topY, placedW, topH));
                    }
                }
            }
        }

        private static List<CutLine> MergeCollinearCuts(List<CutLine> rawCuts, double bladeWidth, double maxTolerance = 0.5)
        {
            if (rawCuts.Count <= 1) return rawCuts;

            var merged = new List<CutLine>();

            // 1. Слияние горизонтальных резов
            var horizontalGroups = rawCuts
                .Where(c => c.Type.Equals("horizontal", StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => Math.Round(c.Y1, 1));

            foreach (var group in horizontalGroups)
            {
                var segments = group.OrderBy(c => c.X1).ToList();
                var current = segments[0];

                for (int i = 1; i < segments.Count; i++)
                {
                    var next = segments[i];

                    // Проверка стыковки или перекрытия с учетом толщины пилы
                    if (next.X1 <= current.X2 + bladeWidth + maxTolerance)
                    {
                        current.X2 = Math.Max(current.X2, next.X2);
                        current.Stage = Math.Min(current.Stage, next.Stage);
                    }
                    else
                    {
                        merged.Add(current);
                        current = next;
                    }
                }
                merged.Add(current);
            }

            // 2. Слияние вертикальных резов
            var verticalGroups = rawCuts
                .Where(c => c.Type.Equals("vertical", StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => Math.Round(c.X1, 1));

            foreach (var group in verticalGroups)
            {
                var segments = group.OrderBy(c => c.Y1).ToList();
                var current = segments[0];

                for (int i = 1; i < segments.Count; i++)
                {
                    var next = segments[i];

                    if (next.Y1 <= current.Y2 + bladeWidth + maxTolerance)
                    {
                        current.Y2 = Math.Max(current.Y2, next.Y2);
                        current.Stage = Math.Min(current.Stage, next.Stage);
                    }
                    else
                    {
                        merged.Add(current);
                        current = next;
                    }
                }
                merged.Add(current);
            }

            return merged
                .OrderBy(c => c.Stage)
                .ThenBy(c => c.Type == "vertical" ? c.X1 : c.Y1)
                .ToList();
        }

        private static List<CutLine> GenerateTrimmingCuts(
            Sheet sheet, TrimmingSettings trimming,
            double trimL, double trimR, double trimB, double trimT)
        {
            var trimLines = new List<CutLine>();

            // Теперь рисуем линию отступа, если сам рассчитанный отступ > 0.
            // Это гарантирует корректную визуализацию новых асимметричных полей.
            if (trimL > 0)
            {
                trimLines.Add(new CutLine
                {
                    X1 = Math.Round(trimL, 2),
                    Y1 = 0,
                    X2 = Math.Round(trimL, 2),
                    Y2 = sheet.Length,
                    Type = "vertical",
                    Stage = 0,
                    IsVisible = true,
                    IsCut = false // Это граница, а не рез детали
                });
            }

            if (trimR > 0)
            {
                trimLines.Add(new CutLine
                {
                    X1 = Math.Round(sheet.Width - trimR, 2),
                    Y1 = 0,
                    X2 = Math.Round(sheet.Width - trimR, 2),
                    Y2 = sheet.Length,
                    Type = "vertical",
                    Stage = 0,
                    IsVisible = true,
                    IsCut = false
                });
            }

            if (trimB > 0)
            {
                trimLines.Add(new CutLine
                {
                    X1 = 0,
                    Y1 = Math.Round(trimB, 2),
                    X2 = sheet.Width,
                    Y2 = Math.Round(trimB, 2),
                    Type = "horizontal",
                    Stage = 0,
                    IsVisible = true,
                    IsCut = false
                });
            }

            if (trimT > 0)
            {
                trimLines.Add(new CutLine
                {
                    X1 = 0,
                    Y1 = Math.Round(sheet.Length - trimT, 2),
                    X2 = sheet.Width,
                    Y2 = Math.Round(sheet.Length - trimT, 2),
                    Type = "horizontal",
                    Stage = 0,
                    IsVisible = true,
                    IsCut = false
                });
            }

            return trimLines;
        }

        private static List<Detail> ExpandDetails(List<Detail> details) =>
            details.SelectMany(d => Enumerable.Range(0, d.Quantity).Select(_ => new Detail
            {
                Id = Guid.NewGuid().ToString(),
                Name = d.Name,
                Length = d.Length,
                Width = d.Width,
                CanRotate = d.CanRotate,
                Quantity = 1
            })).ToList();

        private static void CalculateTotals(CuttingPlan plan)
        {
            double totalSheetArea = plan.SheetLayouts.Sum(sl => sl.Sheet.Width * sl.Sheet.Length);
            double totalUsedArea = plan.SheetLayouts.Sum(sl => sl.Details.Sum(d => d.Width * d.Length));

            if (totalSheetArea > 0)
            {
                plan.TotalMaterialUsage = totalUsedArea / totalSheetArea;
                plan.TotalWastePercentage = 100.0 - (plan.TotalMaterialUsage * 100.0);
            }
        }
    }



}
