using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.DTOs
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public int CategoryId { get; set; }
    }
}
