using Microsoft.EntityFrameworkCore;
using static Cutter.Data.DBModels;

namespace Cutter.Data
{
    /*public class DBContext : DbContext
    {

        public DBContext(DbContextOptions<DBContext> options)
        : base(options)
        {
            //Database.EnsureCreated();
        }

        public DbSet<Items> Items { get; set; }
        public DbSet<CutPlan> CutPlan {  get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Items>()
                .HasIndex(p => new { p.BarCode, p.SKU })
                .IsUnique();
        }

    }*/
}
