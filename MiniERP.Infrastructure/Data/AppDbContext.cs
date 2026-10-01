using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniERP.Domain.Entities;
using MiniERP.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<AiSearchLog> AiSearchLogs => Set<AiSearchLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity tables ke liye zaroori — pehle call karein

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Seed data — yeh part check karein
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Groceries" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Price = 120000, StockQuantity = 15, CategoryId = 1 },
                new Product { Id = 2, Name = "Mouse", Price = 1500, StockQuantity = 5, CategoryId = 1 },
                new Product { Id = 3, Name = "Rice Bag (5kg)", Price = 800, StockQuantity = 50, CategoryId = 2 }
            );

            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "Ali Khan", Email = "ali@example.com" }
            );
        }
    }
}
