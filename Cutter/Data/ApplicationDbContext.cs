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
        public DbSet<CutPlan> CutPlan { get; set; }
        public DbSet<StoreItem> StoreItems { get; set; }
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
        }
    }
}
