using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using MiniERP.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace MiniERP.Infrastructure.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly AppDbContext _context;

        public RateLimitService(AppDbContext context) => _context = context;

        public async Task<bool> CanSearchAsync(string userId, int dailyLimit = 2)
        {
            var today = DateTime.UtcNow.Date;
            var countToday = await _context.AiSearchLogs
                .CountAsync(l => l.UserId == userId && l.SearchedAt >= today);
            return countToday < dailyLimit;
        }

        public async Task<bool> IsGlobalLimitReachedAsync(int globalDailyLimit = 100)
        {
            var today = DateTime.UtcNow.Date;
            var totalToday = await _context.AiSearchLogs.CountAsync(l => l.SearchedAt >= today);
            return totalToday >= globalDailyLimit;
        }

        public async Task LogSearchAsync(string userId)
        {
            _context.AiSearchLogs.Add(new AiSearchLog { UserId = userId });
            await _context.SaveChangesAsync();
        }
    }
}
