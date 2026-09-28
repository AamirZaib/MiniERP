using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 10;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public bool IsLowStock => StockQuantity <= LowStockThreshold;
    }
}
