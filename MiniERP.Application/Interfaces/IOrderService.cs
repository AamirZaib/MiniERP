using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string userId, string userEmail);
        Task<OrderResponse?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderResponse>> GetAllOrdersAsync();
        Task UpdateOrderStatusAsync(int orderId, string newStatus, string userId, string userEmail);
    }
}
