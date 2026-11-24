using Cutter.Data;
using Microsoft.EntityFrameworkCore;

namespace Cutter.Services
{
    public interface IStoreDepartmentService
    {
        Task<List<Store>> GetActiveStoresAsync();
        Task<List<StoreDepartment>> GetDepartmentsAsync(Guid storeId, string? search = null, bool includeInactive = false);
        Task<StoreDepartment?> GetDepartmentByIdAsync(int id);
        Task<StoreDepartment> SaveDepartmentAsync(StoreDepartment department);
        Task<bool> DeleteDepartmentAsync(int id);
        Task<StoreSubdepartment?> GetSubdepartmentByIdAsync(int id);
        Task<List<StoreSubdepartment>> GetSubdepartmentsAsync(int departmentId, bool includeInactive = false);
        Task<StoreSubdepartment> SaveSubdepartmentAsync(StoreSubdepartment subdepartment);
        Task<bool> DeleteSubdepartmentAsync(int id);
        Task<List<StoreDepartment>> GetDepartmentsWithSubdepartmentsAsync(Guid storeId);
    }

    public class StoreDepartmentService : IStoreDepartmentService
    {
        private readonly ApplicationDbContext _db;

        public StoreDepartmentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Store>> GetActiveStoresAsync()
        {
            return await _db.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<StoreDepartment>> GetDepartmentsAsync(Guid storeId, string? search = null, bool includeInactive = false)
        {
            // 1. Загружаем отделы БЕЗ трекинга (чтобы EF не кешил объекты)
            var deptQuery = _db.StoreDepartments
                .AsNoTracking()
                .Where(d => d.StoreId == storeId);

            if (!includeInactive)
            {
                deptQuery = deptQuery.Where(d => d.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                deptQuery = deptQuery.Where(d =>
                    d.Code.ToLower().Contains(search) ||
                    d.Name.ToLower().Contains(search) ||
                    d.Subdepartments.Any(s =>
                        s.Code.ToLower().Contains(search) ||
                        s.Name.ToLower().Contains(search)));
            }

            var departments = await deptQuery
                .OrderBy(d => d.Code)
                .ToListAsync();

            if (!departments.Any()) return departments;

            // 2. Загружаем подотделы отдельным запросом (тоже без трекинга)
            var deptIds = departments.Select(d => d.Id).ToList();

            var subdeptQuery = _db.StoreSubdepartments
                .AsNoTracking()
                .Where(s => deptIds.Contains(s.StoreDepartmentId));

            if (!includeInactive)
            {
                subdeptQuery = subdeptQuery.Where(s => s.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                subdeptQuery = subdeptQuery.Where(s =>
                    s.Code.ToLower().Contains(search) ||
                    s.Name.ToLower().Contains(search));
            }

            var subdepartments = await subdeptQuery
                .OrderBy(s => s.Code)
                .ToListAsync();

            // 3. Вручную связываем подотделы с отделами
            foreach (var dept in departments)
            {
                // ВАЖНО: Очищаем коллекцию перед добавлением!
                dept.Subdepartments.Clear();

                var deptSubs = subdepartments.Where(s => s.StoreDepartmentId == dept.Id).ToList();
                foreach (var sub in deptSubs)
                {
                    dept.Subdepartments.Add(sub);
                }
            }

            return departments;
        }

        public async Task<StoreDepartment?> GetDepartmentByIdAsync(int id)
        {
            return await _db.StoreDepartments
                .Include(d => d.Subdepartments)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<StoreDepartment> SaveDepartmentAsync(StoreDepartment department)
        {
            if (department.Id == 0)
            {
                // Новый отдел
                department.CreatedAt = DateTime.UtcNow;
                _db.StoreDepartments.Add(department);
            }
            else
            {
                // Обновление
                var existing = await _db.StoreDepartments.FindAsync(department.Id);
                if (existing != null)
                {
                    existing.Code = department.Code;
                    existing.Name = department.Name;
                    existing.IsActive = department.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return department;
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            var department = await _db.StoreDepartments
                .Include(d => d.Subdepartments)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null) return false;

            // Проверяем, есть ли товары в подотделах этого отдела
            var hasItems = await _db.StoreItems
                .AnyAsync(si => department.Subdepartments
                    .Select(s => s.Id)
                    .Contains(si.StoreSubdepartmentId ?? 0));

            if (hasItems)
            {
                // Есть товары - только деактивируем
                department.IsActive = false;
                department.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }

            // Нет товаров - удаляем физически вместе с подотделами
            _db.StoreSubdepartments.RemoveRange(department.Subdepartments);
            _db.StoreDepartments.Remove(department);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<StoreSubdepartment>> GetSubdepartmentsAsync(int departmentId, bool includeInactive = false)
        {
            var query = _db.StoreSubdepartments
                .Where(s => s.StoreDepartmentId == departmentId);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderBy(s => s.Code).ToListAsync();
        }

        public async Task<StoreSubdepartment?> GetSubdepartmentByIdAsync(int id)
        {
            return await _db.StoreSubdepartments.FindAsync(id);
        }

        public async Task<StoreSubdepartment> SaveSubdepartmentAsync(StoreSubdepartment subdepartment)
        {
            if (subdepartment.Id == 0)
            {
                // Новый подотдел
                subdepartment.CreatedAt = DateTime.UtcNow;
                _db.StoreSubdepartments.Add(subdepartment);
            }
            else
            {
                // Обновление
                var existing = await _db.StoreSubdepartments.FindAsync(subdepartment.Id);
                if (existing != null)
                {
                    existing.Code = subdepartment.Code;
                    existing.Name = subdepartment.Name;
                    existing.IsActive = subdepartment.IsActive;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return subdepartment;
        }

        public async Task<bool> DeleteSubdepartmentAsync(int id)
        {
            var subdepartment = await _db.StoreSubdepartments
                .Include(s => s.StoreItems)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subdepartment == null) return false;

            // Проверяем, есть ли товары
            if (subdepartment.StoreItems.Any())
            {
                // Есть товары - деактивируем
                subdepartment.IsActive = false;
                subdepartment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }

            // Нет товаров - удаляем физически
            _db.StoreSubdepartments.Remove(subdepartment);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<StoreDepartment>> GetDepartmentsWithSubdepartmentsAsync(Guid storeId)
        {
            return await _db.StoreDepartments
                .Include(d => d.Subdepartments)
                .Where(d => d.StoreId == storeId && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}