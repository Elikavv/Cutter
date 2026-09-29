using Microsoft.EntityFrameworkCore;
using Cutter.Data;

namespace Cutter.Services
{
    public interface IConstraintProfileService
    {
        Task<List<CuttingConstraintProfile>> GetProfilesAsync(Guid storeId);
        Task<CuttingConstraintProfile> GetDefaultProfileAsync(Guid storeId);
        Task SaveProfileAsync(CuttingConstraintProfile profile);
        Task SetDefaultAsync(int profileId, Guid storeId);
        Task DeleteAsync(int profileId);
        Task<List<(Guid Id, string Name)>> GetAllStoresAsync();
    }

    public class ConstraintProfileService : IConstraintProfileService
    {
        private readonly ApplicationDbContext _db;

        public ConstraintProfileService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<CuttingConstraintProfile>> GetProfilesAsync(Guid storeId)
        {
            return await _db.CuttingConstraintProfiles
                .AsNoTracking()
                .Where(p => p.StoreId == storeId)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<CuttingConstraintProfile> GetDefaultProfileAsync(Guid storeId)
        {
            var defaultProfile = await _db.CuttingConstraintProfiles
                .FirstOrDefaultAsync(p => p.StoreId == storeId && p.IsDefault);

            if (defaultProfile == null)
            {
                // Если профиля нет, создаем дефолтный "на лету" (но не сохраняем в БД, пока юзер не нажмет "Сохранить")
                defaultProfile = new CuttingConstraintProfile
                {
                    StoreId = storeId,
                    Name = "Стандартный",
                    IsDefault = true,
                    BladeWidth = 3.2,
                    CanRotateParts = true
                };
            }
            return defaultProfile;
        }

        public async Task SaveProfileAsync(CuttingConstraintProfile profile)
        {
            if (profile.IsDefault)
            {
                var others = await _db.CuttingConstraintProfiles
                    .Where(p => p.StoreId == profile.StoreId && p.Id != profile.Id)
                    .ToListAsync();

                foreach (var p in others) p.IsDefault = false;
            }

            if (profile.Id == 0)
            {
                // Новый профиль
                _db.CuttingConstraintProfiles.Add(profile);
            }
            else
            {
                var tracked = _db.ChangeTracker.Entries<CuttingConstraintProfile>()
                                       .FirstOrDefault(e => e.Entity.Id == profile.Id);
                if (tracked != null)
                {
                    _db.Entry(tracked.Entity).State = EntityState.Detached;
                }

                // Существующий профиль - обновляем
                var existing = await _db.CuttingConstraintProfiles.FindAsync(profile.Id);
                if (existing == null) throw new Exception("Профиль не найден");

                // Обновляем ТОЛЬКО изменяемые поля (НЕ Id и НЕ StoreId!)
                existing.Name = profile.Name;
                existing.IsDefault = profile.IsDefault;
                existing.BladeWidth = profile.BladeWidth;
                existing.MarginTop = profile.MarginTop;
                existing.MarginBottom = profile.MarginBottom;
                existing.MarginLeft = profile.MarginLeft;
                existing.MarginRight = profile.MarginRight;
                existing.MinDistanceBetweenParts = profile.MinDistanceBetweenParts;
                existing.CanRotateParts = profile.CanRotateParts;
                existing.MaxCutLength = profile.MaxCutLength;

                // НЕ вызываем Update(), EF сам отследит изменения
            }

            await _db.SaveChangesAsync();
        }

        public async Task SetDefaultAsync(int profileId, Guid storeId)
        {
            var profiles = await _db.CuttingConstraintProfiles
                .Where(p => p.StoreId == storeId)
                .ToListAsync();

            foreach (var p in profiles)
            {
                p.IsDefault = (p.Id == profileId);
            }
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int profileId)
        {
            var profile = await _db.CuttingConstraintProfiles.FindAsync(profileId);
            if (profile != null)
            {
                _db.CuttingConstraintProfiles.Remove(profile);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<(Guid Id, string Name)>> GetAllStoresAsync()
        {
            var stores = await _db.Stores
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            return stores.Select(s => (s.Id, s.Name)).ToList();
        }
    }
}