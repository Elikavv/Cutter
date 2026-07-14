using Microsoft.AspNetCore.Identity;

namespace Cutter.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Проверяем, есть ли уже магазины
            if (context.Stores.Any()) return; // База уже инициализирована

            // 2. Создаём магазин
            var store = new Store
            {
                Name = "Леруа 1",
                Address = "г. Набережные Челны, ул. Машиностроительная, 29",
                IsActive = true,
                SubscriptionExpiresAt = DateTime.Now.AddYears(1) // активна 1 год
            };

            context.Stores.Add(store);
            await context.SaveChangesAsync(); // ⚠️ Сохраняем, чтобы получить Store.Id

            // 3. Создаём администратора
            var adminEmail = "admin@cutter.ru";
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                StoreId = store.Id,
                Role = "Admin"
            };

            var result = await userManager.CreateAsync(adminUser, "Password123!");

            if (!result.Succeeded)
            {
                throw new Exception($"Не удалось создать администратора: {string.Join(", ", result.Errors)}");
            }

            // 4. (Опционально) Добавим в роль Identity, если используешь
            // Это полезно, если ты используешь [Authorize(Roles = "Admin")]
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
