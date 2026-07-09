using System;
using Microsoft.EntityFrameworkCore;
using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Data
{
    public class LocalDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<BeanBag> BeanBags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=pos_offline.db");
    }
}
