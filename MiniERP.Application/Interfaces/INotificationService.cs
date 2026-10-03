using MiniERP.Application.DTOs;
using MiniERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface INotificationService
    {
        Task CreateLowStockNotificationAsync(int productId, string productName, int currentStock);
        Task<IEnumerable<NotificationResponse>> GetRecentAsync(int count = 20);
        Task<int> GetUnreadCountAsync();
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync();
    }
}
