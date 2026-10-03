using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Entities
{
    public enum PurchaseOrderStatus
    {
        Pending,
        Ordered,
        Received,
        Cancelled
    }

    public class PurchaseOrder
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

        public decimal TotalAmount => Items.Sum(i => i.UnitCost * i.Quantity);
    }
}
