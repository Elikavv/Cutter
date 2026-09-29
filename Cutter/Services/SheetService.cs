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

            return await _context.StoreItems
                .Where(si => si.StoreId == user.StoreId && si.IsActive)
                .OrderBy(si => si.Item.Name)
                .ThenBy(si => si.Item.Length)
                .ThenBy(si => si.Item.Width)
                .Select(si => new Sheet
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
                    CutPrice = si.Item.MaterialType.StoreMaterialCutPrices.FirstOrDefault(x => x.MaterialTypeId == si.Item.MaterialTypeId).CutPrice,
                })
                .ToListAsync();
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
