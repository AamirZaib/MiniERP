using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
