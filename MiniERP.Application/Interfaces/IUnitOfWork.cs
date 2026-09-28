using MiniERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<Customer> Customers { get; }
        IGenericRepository<Category> Categories { get; }

        Task<int> SaveChangesAsync(); // yeh actual database mein commit karta hai
    }
}
