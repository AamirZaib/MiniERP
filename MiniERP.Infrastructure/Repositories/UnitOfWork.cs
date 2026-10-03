using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using MiniERP.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Products = new GenericRepository<Product>(_context);
            Orders = new GenericRepository<Order>(_context);
            Customers = new GenericRepository<Customer>(_context);
            Categories = new GenericRepository<Category>(_context);
            Suppliers = new GenericRepository<Supplier>(_context);
            PurchaseOrders = new GenericRepository<PurchaseOrder>(_context);
            Invoices = new GenericRepository<Invoice>(_context);
            Notifications = new GenericRepository<Notification>(_context);
        }

        public IGenericRepository<Product> Products { get; }
        public IGenericRepository<Order> Orders { get; }
        public IGenericRepository<Customer> Customers { get; }
        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Supplier> Suppliers { get; }
        public IGenericRepository<PurchaseOrder> PurchaseOrders { get; }
        public IGenericRepository<Invoice> Invoices { get; }
        public IGenericRepository<Notification> Notifications { get; }
        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
