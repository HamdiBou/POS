using System;
using System.Collections.Generic;
using System.IO;
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

        public LocalDbContext()
        {
            Database.EnsureCreated();
            EnsureColumn("Articles", "IsSellable");
        }

        private void EnsureColumn(string tableName, string columnName)
        {
            try
            {
                var connection = Database.GetDbConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info('{tableName}')";
                using var reader = command.ExecuteReader();

                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    columns.Add(reader.GetString(1));
                }

                if (!columns.Contains(columnName))
                {
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} BOOLEAN NOT NULL DEFAULT 1";
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to ensure column {columnName} on {tableName}: {ex.Message}");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pos_offline.db");
            options.UseSqlite($"Data Source={dbPath}");
        }

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
