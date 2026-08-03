using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System;
using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Если магазинов вообще нет, создаем тестовый
            if (!context.Stores.Any())
            {
                var store = new Store
                {
                    Name = "Леруа 1",
                    Address = "г. Набережные Челны, ул. Машиностроительная, 29",
                    IsActive = true,
                    SubscriptionExpiresAt = DateTime.Now.AddYears(1)
                };
                context.Stores.Add(store);
                await context.SaveChangesAsync();
            }

            var stores = context.Stores.Where(s => s.IsActive).ToList();

            // 3. Создаём Типы материалов (если нет)
            if (!context.MaterialTypes.Any())
            {
                var materialTypes = new List<MaterialType>
                {
                    new() { MaterialName = "Листовой материал", Descr = "ДСП, ЛДСП, ДВП, ОСБ, фанера, мебельный щит, МДФ, декоративный профиль" },
                    new() { MaterialName = "Столешница", Descr = "Кухонные и офисные столешницы" },
                    new() { MaterialName = "Брус. Доска. Погонаж", Descr = "Наличник, вагонка, брус, добор, доска, цоколь, рейка, поручень, балясина, европол, половая доска, столб" },
                    new() { MaterialName = "Пластиковый профиль", Descr = "Плинтус, наличник, профиль, труба" },
                    new() { MaterialName = "Пластиковые изделия", Descr = "Панель, откос, подоконник, сайдинг, поликарбонат" }
                };
                context.MaterialTypes.AddRange(materialTypes);
                await context.SaveChangesAsync();
            }

            var allMaterialTypes = context.MaterialTypes.ToList();

            // 4. Создаём Услуги резки (если нет)
            if (!context.CuttingServices.Any())
            {
                var services = new List<CuttingServiceModel>
                {
                    // Основные услуги
                    new() { Name = "Брус. Доска. Погонаж", Description = "Наличник, вагонка, брус, добор, доска, цоколь, рейка, поручень, балясина, европол, половая доска, столб", IsExtraService = false, SortOrder = 1 },
                    new() { Name = "Пластиковый профиль", Description = "Плинтус, наличник, профиль, труба", IsExtraService = false, SortOrder = 2 },
                    new() { Name = "Пластиковые изделия", Description = "Панель, откос, подоконник, сайдинг, поликарбонат", IsExtraService = false, SortOrder = 3 },
                    new() { Name = "Листовой материал", Description = "ДСП, ЛДСП, ДВП, ОСБ, фанера, мебельный щит, подоконник, стеновая панель, МДФ, декоративный профиль", IsExtraService = false, SortOrder = 4 },
                    new() { Name = "Столешница", Description = "", IsExtraService = false, SortOrder = 5 },
                    
                    // Дополнительные услуги (флаг оставлен на будущее)
                    new() { Name = "Отверстие в мойке", Description = "", IsExtraService = true, SortOrder = 1 },
                    new() { Name = "Отверстие под мебельные петли", Description = "", IsExtraService = true, SortOrder = 2 },
                    new() { Name = "Вырез под мойку/варочную панель", Description = "Вырез осуществляется после предоставления мойки или варочной панели", IsExtraService = true, SortOrder = 3 }
                };
                context.CuttingServices.AddRange(services);
                await context.SaveChangesAsync();
            }

            var allServices = context.CuttingServices.ToList();

            // 5. ПРИВЯЗКА: Назначаем услугу по умолчанию для каждого типа материала
            // Проверяем, есть ли уже привязки
            bool hasLinks = allMaterialTypes.Any(m => m.DefaultCuttingServiceId.HasValue);
            if (!hasLinks)
            {
                foreach (var matType in allMaterialTypes)
                {
                    // Ищем услугу по совпадению начала названия (не доп. услугу)
                    var defaultService = allServices.FirstOrDefault(s =>
                        !s.IsExtraService &&
                        s.Name.StartsWith(matType.MaterialName.Split('.')[0].Trim()));

                    if (defaultService != null)
                    {
                        matType.DefaultCuttingServiceId = defaultService.Id;
                    }
                }
                await context.SaveChangesAsync();
            }

            // 6. Создаём цены на услуги для каждого магазина (если нет)
            foreach (var store in stores)
            {
                if (!context.StoreServicePrices.Any(p => p.StoreId == store.Id))
                {
                    var storePrices = new List<StoreServicePrice>();
                    foreach (var service in allServices)
                    {
                        // Определяем цену по названию услуги (как в прайсе Лемана)
                        float price = service.Name switch
                        {
                            "Брус. Доска. Погонаж" => 40f,
                            "Пластиковый профиль" => 55f,
                            "Пластиковые изделия" => 55f,
                            "Листовой материал" => 70f,
                            "Столешница" => 180f,
                            "Отверстие в мойке" => 600f,
                            "Отверстие под мебельные петли" => 80f,
                            "Вырез под мойку/варочную панель" => 800f,
                            _ => 70f
                        };

                        storePrices.Add(new StoreServicePrice
                        {
                            StoreId = store.Id,
                            ServiceId = service.Id,
                            Price = price,
                            IsActive = true
                        });
                    }
                    context.StoreServicePrices.AddRange(storePrices);
                }
            }
            await context.SaveChangesAsync();

            // 4. Создаём администратора, если его еще нет
            /*var adminEmail = "admin@cutter.ru";
            if (!context.Users.Any(u => u.Email == adminEmail))
            {
                var defaultStore = stores.FirstOrDefault(); // Привязываем к первому попавшемуся магазину

                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    StoreId = defaultStore.Id,
                    Role = "Admin"
                };

                var result = await userManager.CreateAsync(adminUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }*/
        }
    }
}