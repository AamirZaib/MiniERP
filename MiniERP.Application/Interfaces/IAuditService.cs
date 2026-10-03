using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string userEmail, string action, string entityType, string? entityId, string details);
        Task<IEnumerable<AuditLogResponse>> GetRecentAsync(int count = 50);
    }
}
