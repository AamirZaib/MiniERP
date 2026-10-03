using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;       // e.g. "OrderCreated", "ProductCreated"
        public string EntityType { get; set; } = string.Empty;   // e.g. "Order", "Product"
        public string? EntityId { get; set; }                    // e.g. "5"
        public string Details { get; set; } = string.Empty;      // e.g. "Order #5 created for Ali Khan, Total Rs.4500"
    }
}
