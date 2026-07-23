using System;
using Microsoft.EntityFrameworkCore;
using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Data
{
    public class LocalDbContext : DbContext
    {
        public DbSet<LocalArticle> Articles { get; set; }
        public DbSet<LocalArticleCost> ArticleCosts { get; set; }
        public DbSet<LocalShift> Shifts { get; set; }
        public DbSet<LocalOrder> Orders { get; set; }
        public DbSet<LocalOrderItem> OrderItems { get; set; }
        public DbSet<LocalBeanBag> BeanBags { get; set; }
        public DbSet<LocalSetting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=pos_offline.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalArticle>().HasKey(a => a.Id);
            modelBuilder.Entity<LocalArticleCost>().HasKey(ac => ac.ArticleId);
            modelBuilder.Entity<LocalShift>().HasKey(s => s.Id);
            modelBuilder.Entity<LocalOrder>().HasKey(o => o.Id);
            modelBuilder.Entity<LocalOrderItem>().HasKey(oi => oi.Id);
            modelBuilder.Entity<LocalBeanBag>().HasKey(bb => bb.Id);
            modelBuilder.Entity<LocalSetting>().HasKey(s => s.Key);
        }
    }
}
