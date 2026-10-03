using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "LowStock"; // future mein aur types ho sakte hain
        public int? RelatedProductId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
