using Cutter.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Cutter.Data.DBModels;

namespace Cutter.Services
{
    public class MaterialTypeDto
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string? Descr { get; set; }
        public int? DefaultCuttingServiceId { get; set; }
        public string? DefaultServiceName { get; set; }
    }

    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsExtraService { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ServicePriceDto
    {
        public Guid? Id { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceCode { get; set; }
        public string? Description { get; set; }
        public bool IsExtraService { get; set; }
        public float Price { get; set; }
    }

    public class AvailableServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceCode { get; set; }
        public string? Description { get; set; }
        public bool IsExtraService { get; set; }
    }

    public interface IStorePricingService
    {
        Task<List<MaterialTypeDto>> GetAllMaterialTypesAsync();
        Task<MaterialTypeDto> SaveMaterialTypeAsync(MaterialTypeDto dto);
        Task DeleteMaterialTypeAsync(int id);

        Task<List<ServiceDto>> GetAllServicesAsync();
        Task<ServiceDto> SaveServiceAsync(ServiceDto dto);
        Task DeleteServiceAsync(int serviceId);

        Task<List<Store>> GetAllStoresAsync();
        Task<Guid?> GetUserStoreIdAsync(string userId);

        Task<List<ServicePriceDto>> GetStorePricesAsync(Guid storeId);
        Task<List<AvailableServiceDto>> GetAvailableServicesForStoreAsync(Guid storeId);
        Task AddServiceToStoreAsync(Guid storeId, int serviceId, float price, string? serviceCode = null);
        Task UpdateServicePriceAsync(Guid priceId, float newPrice, string? serviceCode = null);
        Task RemoveServiceFromStoreAsync(Guid priceId);
    }

    public class StorePricingService : IStorePricingService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public StorePricingService(IDbContextFactory<ApplicationDbContext> contextFactory, UserManager<ApplicationUser> userManager)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
        }

        public async Task<List<MaterialTypeDto>> GetAllMaterialTypesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.MaterialTypes
                .Include(m => m.DefaultCuttingService)
                .OrderBy(m => m.MaterialName)
                .Select(m => new MaterialTypeDto
                {
                    Id = m.Id,
                    MaterialName = m.MaterialName,
                    Descr = m.Descr,
                    DefaultCuttingServiceId = m.DefaultCuttingServiceId,
                    DefaultServiceName = m.DefaultCuttingService != null ? m.DefaultCuttingService.Name : null
                }).ToListAsync();
        }

        public async Task<MaterialTypeDto> SaveMaterialTypeAsync(MaterialTypeDto dto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (dto.Id == 0)
            {
                var newMaterial = new MaterialType { MaterialName = dto.MaterialName, Descr = dto.Descr, DefaultCuttingServiceId = dto.DefaultCuttingServiceId == 0 ? null : dto.DefaultCuttingServiceId };
                context.MaterialTypes.Add(newMaterial);
                await context.SaveChangesAsync();
                dto.Id = newMaterial.Id;
            }
            else
            {
                var existing = await context.MaterialTypes.FindAsync(dto.Id);
                if (existing != null)
                {
                    existing.MaterialName = dto.MaterialName;
                    existing.Descr = dto.Descr;
                    existing.DefaultCuttingServiceId = dto.DefaultCuttingServiceId == 0 ? null : dto.DefaultCuttingServiceId;
                    await context.SaveChangesAsync();
                }
            }
            return dto;
        }

        public async Task DeleteMaterialTypeAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var material = await context.MaterialTypes.FindAsync(id);
            if (material != null) { context.MaterialTypes.Remove(material); await context.SaveChangesAsync(); }
        }

        public async Task<List<ServiceDto>> GetAllServicesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CuttingServices.Where(s => s.IsActive).OrderBy(s => s.IsExtraService).ThenBy(s => s.SortOrder)
                .Select(s => new ServiceDto { Id = s.Id, Name = s.Name, Description = s.Description, IsExtraService = s.IsExtraService, SortOrder = s.SortOrder, IsActive = s.IsActive }).ToListAsync();
        }

        public async Task<ServiceDto> SaveServiceAsync(ServiceDto dto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (dto.Id == 0)
            {
                var newService = new CuttingServiceModel { Name = dto.Name, Description = dto.Description, IsExtraService = dto.IsExtraService, SortOrder = dto.SortOrder, IsActive = true, ModifyDate = DateTime.Now };
                context.CuttingServices.Add(newService);
                await context.SaveChangesAsync();
                dto.Id = newService.Id;
            }
            else
            {
                var existing = await context.CuttingServices.FindAsync(dto.Id);
                if (existing != null)
                {
                    existing.Name = dto.Name;
                    existing.Description = dto.Description;
                    existing.IsExtraService = dto.IsExtraService;
                    existing.SortOrder = dto.SortOrder;
                    existing.ModifyDate = DateTime.Now;
                    await context.SaveChangesAsync();
                }
            }
            return dto;
        }

        public async Task DeleteServiceAsync(int serviceId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var service = await context.CuttingServices.FindAsync(serviceId);
            if (service != null) { service.IsActive = false; service.ModifyDate = DateTime.Now; await context.SaveChangesAsync(); }
        }

        public async Task<List<Store>> GetAllStoresAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<Guid?> GetUserStoreIdAsync(string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await _userManager.FindByIdAsync(userId);
            return user?.StoreId;
        }

        public async Task<List<ServicePriceDto>> GetStorePricesAsync(Guid storeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await (from price in context.StoreServicePrices
                          where price.StoreId == storeId && price.IsActive
                          join service in context.CuttingServices on price.ServiceId equals service.Id
                          where service.IsActive
                          orderby service.IsExtraService, service.SortOrder
                          select new ServicePriceDto
                          {
                              Id = price.Id,
                              ServiceId = service.Id,
                              ServiceName = service.Name,
                              ServiceCode = price.ServiceCode,
                              Description = service.Description,
                              IsExtraService = service.IsExtraService,
                              Price = price.Price
                          }).ToListAsync();
        }

        public async Task<List<AvailableServiceDto>> GetAvailableServicesForStoreAsync(Guid storeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existingIds = await context.StoreServicePrices.Where(p => p.StoreId == storeId && p.IsActive).Select(p => p.ServiceId).ToListAsync();
            return await context.CuttingServices
                .Where(s => s.IsActive && !existingIds.Contains(s.Id))
                .OrderBy(s => s.IsExtraService).ThenBy(s => s.SortOrder)
                .Select(s => new AvailableServiceDto
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Description = s.Description,
                    IsExtraService = s.IsExtraService
                })
                .ToListAsync();
        }

        public async Task AddServiceToStoreAsync(Guid storeId, int serviceId, float price, string? serviceCode = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.StoreServicePrices.FirstOrDefaultAsync(p => p.StoreId == storeId && p.ServiceId == serviceId);
            if (existing != null) { existing.IsActive = true; existing.Price = price; existing.ServiceCode = serviceCode; }
            else { context.StoreServicePrices.Add(new StoreServicePrice { StoreId = storeId, ServiceId = serviceId, Price = price, ServiceCode = serviceCode, IsActive = true }); }
            await context.SaveChangesAsync();
        }

        public async Task UpdateServicePriceAsync(Guid priceId, float newPrice, string? serviceCode = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var price = await context.StoreServicePrices.FindAsync(priceId);
            if (price != null) { price.Price = newPrice; if (serviceCode != null) price.ServiceCode = serviceCode; await context.SaveChangesAsync(); }
        }

        public async Task RemoveServiceFromStoreAsync(Guid priceId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var price = await context.StoreServicePrices.FindAsync(priceId);
            if (price != null) { price.IsActive = false; await context.SaveChangesAsync(); }
        }
    }
}