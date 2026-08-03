using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Store> Stores { get; set; } = null!;
        public DbSet<Items> Items { get; set; }
        public DbSet<MaterialType> MaterialTypes { get; set; }
        public DbSet<CutPlan> CutPlan { get; set; }
        public DbSet<StoreItem> StoreItems { get; set; }
        public DbSet<CuttingServiceModel> CuttingServices { get; set; }
        public DbSet<StoreServicePrice> StoreServicePrices { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Store)
                .WithMany()
                .HasForeignKey(u => u.StoreId)
                .OnDelete(DeleteBehavior.Restrict); // нельзя удалить магазин, если есть пользователи

            builder.Entity<Items>()
                .HasIndex(p => new { p.BarCode, p.SKU })
                .IsUnique();


            // Составной ключ для StoreItem
            builder.Entity<StoreItem>()
                .HasKey(si => new { si.StoreId, si.ItemId });

            builder.Entity<StoreItem>()
               .HasOne(si => si.Store)
               .WithMany(s => s.StoreItems)
               .HasForeignKey(si => si.StoreId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StoreItem>()
               .HasOne(si => si.Item)
               .WithMany(i => i.StoreItems)
               .HasForeignKey(si => si.ItemId)
               .OnDelete(DeleteBehavior.Cascade);

            // Индексы
            builder.Entity<StoreItem>()
                .HasIndex(si => si.StoreId);

            builder.Entity<StoreItem>()
                .HasIndex(si => si.ItemId);

            // 🔑 Настройка составного первичного ключа
            builder.Entity<StoreMaterialCutPrice>()
                .HasKey(smcp => new { smcp.StoreId, smcp.MaterialTypeId });

            // Связи
            builder.Entity<StoreMaterialCutPrice>()
                .HasOne(smcp => smcp.Store)
                .WithMany(s => s.StoreMaterialCutPrices)
                .HasForeignKey(smcp => smcp.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StoreMaterialCutPrice>()
                .HasOne(smcp => smcp.MaterialType)
                .WithMany(mt => mt.StoreMaterialCutPrices)
                .HasForeignKey(smcp => smcp.MaterialTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индексы
            builder.Entity<StoreMaterialCutPrice>()
                .HasIndex(smcp => smcp.StoreId);

            builder.Entity<StoreMaterialCutPrice>()
                .HasIndex(smcp => smcp.MaterialTypeId);

            // ==========================================
            // 5. НОВЫЕ ТАБЛИЦЫ: Услуги резки и цены
            // ==========================================

            builder.Entity<CuttingServiceModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsExtraService);
            });

            builder.Entity<StoreServicePrice>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Связь с Магазином
                entity.HasOne(e => e.Store)
                      .WithMany(s => s.StoreServicePrices) // Убедитесь, что это свойство есть в классе Store
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Связь с Услугой
                entity.HasOne(e => e.Service)
                      .WithMany()
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ВАЖНО: Запрещаем создавать две цены на одну услугу в одном магазине
                entity.HasIndex(e => new { e.StoreId, e.ServiceId }).IsUnique();
            });

            builder.Entity<MaterialType>()
               .HasOne(m => m.DefaultCuttingService)
               .WithMany(s => s.MaterialTypes)
               .HasForeignKey(m => m.DefaultCuttingServiceId)
               .OnDelete(DeleteBehavior.SetNull); // Если услугу деактивируют/удалят, тип материала останется, но ссылка обнулится

        }
    }
}
