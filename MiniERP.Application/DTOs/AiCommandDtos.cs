using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.DTOs
{
    public class AiCommandRequest
    {
        public string Command { get; set; } = string.Empty;
    }

    public class AiCommandItemProposal
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; } // sirf Purchase Order ke liye
    }

    public class AiCommandProposal
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string ActionType { get; set; } = string.Empty; // "create_purchase_order" ya "create_sales_order"
        public string Summary { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public List<AiCommandItemProposal> Items { get; set; } = new();
    }
}
