using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, string userId, string userEmail);
        Task<IEnumerable<PurchaseOrderResponse>> GetAllAsync();
        Task MarkAsReceivedAsync(int purchaseOrderId, string userId, string userEmail);
    }
}
