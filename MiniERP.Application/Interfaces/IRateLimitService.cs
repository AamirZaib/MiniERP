using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> CanSearchAsync(string userId, int dailyLimit = 5);
        Task LogSearchAsync(string userId);
        Task<bool> IsGlobalLimitReachedAsync(int globalDailyLimit = 100);
    }
}
