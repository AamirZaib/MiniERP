using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt) GenerateToken(string userId, string email, IList<string> roles);
    }
}
