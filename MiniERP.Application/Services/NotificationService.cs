using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public NotificationService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task CreateLowStockNotificationAsync(int productId, string productName, int currentStock)
        {
            // Duplicate spam na ho — agar is product ke liye already ek UNREAD low-stock notification maujood hai, naya na banayein
            var existing = await _unitOfWork.Notifications.GetAllAsync();
            var alreadyNotified = existing.Any(n =>
                n.RelatedProductId == productId && n.Type == "LowStock" && !n.IsRead);

            if (alreadyNotified) return;

            var notification = new Notification
            {
                Message = $"'{productName}' is running low on stock ({currentStock} units remaining).",
                Type = "LowStock",
                RelatedProductId = productId
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<NotificationResponse>> GetRecentAsync(int count = 20)
        {
            var all = await _unitOfWork.Notifications.GetAllAsync();
            return all.OrderByDescending(n => n.CreatedAt).Take(count).Select(n => new NotificationResponse
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var all = await _unitOfWork.Notifications.GetAllAsync();
            return all.Count(n => !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId)
                ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync()
        {
            var all = await _unitOfWork.Notifications.GetAllAsync();
            foreach (var n in all.Where(n => !n.IsRead))
            {
                n.IsRead = true;
                _unitOfWork.Notifications.Update(n);
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
