using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using MiniERP.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace MiniERP.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;
        public AuditService(AppDbContext context) => _context = context;

        public async Task LogAsync(string userId, string userEmail, string action, string entityType, string? entityId, string details)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details
            });
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLogResponse>> GetRecentAsync(int count = 50)
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs.Select(l => new AuditLogResponse
            {
                Id = l.Id,
                Timestamp = l.Timestamp,
                UserEmail = l.UserEmail,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Details = l.Details
            });
        }
    }
}
