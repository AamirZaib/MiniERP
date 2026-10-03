using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.DTOs
{
    public class GlobalSearchResponse
    {
        public string EntityType { get; set; } = string.Empty; // "products", "customers", "orders", "categories", "none"
        public string Explanation { get; set; } = string.Empty;
        public List<ProductResponse> Products { get; set; } = new();
        public List<CustomerResponse> Customers { get; set; } = new();
        public List<OrderResponse> Orders { get; set; } = new();
        public List<CategoryResponse> Categories { get; set; } = new();
    }
}
