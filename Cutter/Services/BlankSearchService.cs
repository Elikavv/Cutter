using Microsoft.EntityFrameworkCore;
using Cutter.Data;

namespace Cutter.Services
{
    public interface IBlankSearchService
    {
        Task<List<BlankSearchResult>> SearchAsync(string userId, string query, int maxResults = 10);
    }

    public class BlankSearchService : IBlankSearchService
    {
        private readonly ApplicationDbContext _dbContext;

        public BlankSearchService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<BlankSearchResult>> SearchAsync(string userId, string query, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<BlankSearchResult>();

            var user = await _dbContext.Users
                .Include(u => u.Store)
                .Include(u => u.ManagerStores)
                    .ThenInclude(ms => ms.Store)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return new List<BlankSearchResult>();

            var queryLower = query.Trim().ToLower();

            // Базовый запрос: ищем по NumCut и Invoice
            var baseQuery = _dbContext.CutPlan
                .Include(cp => cp.User)
                .ThenInclude(u => u.Store)
                .Where(cp => cp.IsSave &&
                             ((cp.NumCut != null && cp.NumCut.ToLower().Contains(queryLower)) ||
                              (cp.Invoice != null && cp.Invoice.ToLower().Contains(queryLower))));

            // Фильтрация по правам
            if (user.Role == "AdminSuper")
            {
                // Супер-админ видит всё
            }
            else if (user.Role == "AdminStore" || user.Role == "Cashier")
            {
                // Админ магазина и кассир видят только бланки своего магазина
                var userStoreId = user.StoreId;
                baseQuery = baseQuery.Where(cp => cp.User != null && cp.User.StoreId == userStoreId);
            }
            else if (user.Role == "Manager")
            {
                // Менеджер видит бланки всех распиловщиков в своих магазинах
                // Собираем все StoreId менеджера
                var managerStoreIds = new List<Guid> { user.StoreId }; // Основной магазин

                // Добавляем магазины из ManagerStores
                if (user.ManagerStores != null)
                {
                    managerStoreIds.AddRange(user.ManagerStores.Select(ms => ms.StoreId));
                }

                // Убираем дубликаты
                managerStoreIds = managerStoreIds.Distinct().ToList();

                // Фильтруем бланки по этим магазинам
                baseQuery = baseQuery.Where(cp => cp.User != null && managerStoreIds.Contains(cp.User.StoreId));
            }
            else
            {
                // Распиловщик (User) и другие видят только свои бланки
                baseQuery = baseQuery.Where(cp => cp.UserId == userId);
            }

            var results = await baseQuery
                .OrderByDescending(cp => cp.CreateDate)
                .Take(maxResults)
                .Select(cp => new BlankSearchResult
                {
                    GUUID = cp.GUUID,
                    NumCut = cp.NumCut ?? "",
                    Invoice = cp.Invoice ?? "",
                    CreateDate = cp.CreateDate,
                    IsPaid = cp.IsPaid,
                    CreatorName = cp.User != null ? cp.User.FullName : "Неизвестно",
                    StoreName = cp.User != null && cp.User.Store != null ? cp.User.Store.Name : ""
                })
                .ToListAsync();

            return results;
        }
    }

    public class BlankSearchResult
    {
        public string GUUID { get; set; } = "";
        public string NumCut { get; set; } = "";
        public string Invoice { get; set; } = "";
        public DateTime CreateDate { get; set; }
        public bool IsPaid { get; set; }
        public string CreatorName { get; set; } = "";
        public string StoreName { get; set; } = "";
    }
}