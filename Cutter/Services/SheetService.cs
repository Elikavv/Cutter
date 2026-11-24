using Cutter.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using static Cutter.Data.DBModels;

namespace Cutter.Services
{
    public class SheetService
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SheetService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<Sheet>> GetActiveSheetsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.StoreId == null)
                return new List<Sheet>();

            // 1. Загружаем из БД (EF Core транслирует это в SQL)
            var storeItems = await _context.StoreItems
                .Where(si => si.StoreId == user.StoreId && si.IsActive)
                .Include(si => si.Item)
                    .ThenInclude(i => i.MaterialType)
                        .ThenInclude(mt => mt.StoreMaterialCutPrices)
                .OrderBy(si => si.Item.Name)
                .ThenBy(si => si.Item.Length)
                .ThenBy(si => si.Item.Width)
                .ToListAsync();

            // 2. Проецируем в памяти (тут можно использовать ?.)
            return storeItems.Select(si => new Sheet
            {
                Name = si.Item.Name,
                BarCode = si.Item.BarCode,
                SKU = si.Item.SKU,
                Length = si.Item.Length,
                Width = si.Item.Width,
                Depth = si.Item.Depth,
                Id = si.Item.Id,
                Price = si.Price,
                MaterialTypeId = si.Item.MaterialTypeId,
                CutPrice = si.Item.MaterialType?.StoreMaterialCutPrices
                    .FirstOrDefault(x => x.MaterialTypeId == si.Item.MaterialTypeId)?.CutPrice ?? 0,
            }).ToList();
        }

        public async Task<List<Sheet>> SearchSheetsByNameAsync(string userId, string searchTerm, int maxResults = 20)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.StoreId == null) return new List<Sheet>();

            // 1. Делаем запрос к БД с фильтром, Include и Take(20)
            var storeItems = await _context.StoreItems
                .Where(si => si.StoreId == user.StoreId && si.IsActive)
                .Include(si => si.Item)
                    .ThenInclude(i => i.MaterialType)
                        .ThenInclude(mt => mt.StoreMaterialCutPrices)
                .Where(si => si.Item.Name.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(si => si.Item.Name)
                .Take(maxResults)
                .ToListAsync();

            // 2. Проекцию делаем в памяти, чтобы безопасно использовать ?. для CutPrice
            return storeItems.Select(si => new Sheet
            {
                Name = si.Item.Name,
                BarCode = si.Item.BarCode,
                SKU = si.Item.SKU,
                Length = si.Item.Length,
                Width = si.Item.Width,
                Depth = si.Item.Depth,
                Id = si.Item.Id,
                Price = si.Price,
                MaterialTypeId = si.Item.MaterialTypeId,
                CutPrice = si.Item.MaterialType?.StoreMaterialCutPrices
                    .FirstOrDefault(x => x.MaterialTypeId == si.Item.MaterialTypeId)?.CutPrice ?? 0,
            }).ToList();
        }

        public async Task<List<Sheet>> GetSheetsByIdsAsync(List<int> ids)
        {
            return await _context.Items
                .Where(s => ids.Contains(s.Id))
                .Select(x => new Sheet
                {
                    Name = x.Name,
                    BarCode = x.BarCode,
                    SKU = x.SKU,
                    Length = x.Length,
                    Width = x.Width,
                    Depth = x.Depth,
                    Id = x.Id,
                    Price = x.Price,
                    MaterialTypeId = x.MaterialTypeId,
                    CutPrice = x.MaterialType.StoreMaterialCutPrices.FirstOrDefault(z => z.MaterialTypeId == x.MaterialTypeId).CutPrice
                })
                .ToListAsync();
        }

        public async Task<Sheet> GetSheetByCode(string code)
        {
            return await _context.Items
                .Where(s => s.IsActive && (code.Contains(s.SKU) || code.Contains(s.BarCode)))
                .Select(x => new Sheet
                {
                    Name = x.Name,
                    BarCode = x.BarCode,
                    SKU = x.SKU,
                    Length = x.Length,
                    Width = x.Width,
                    Depth = x.Depth,
                    Id = x.Id,
                    Price = x.Price,
                    MaterialTypeId = x.MaterialTypeId,
                    CutPrice = x.MaterialType.StoreMaterialCutPrices.FirstOrDefault(z => z.MaterialTypeId == x.MaterialTypeId).CutPrice
                })
                .FirstOrDefaultAsync();
        }
    }
}
