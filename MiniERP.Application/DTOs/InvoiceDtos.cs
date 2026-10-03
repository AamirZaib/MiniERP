using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.DTOs
{
    public class InvoiceResponse
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public int OrderId { get; set; }
    }
}
