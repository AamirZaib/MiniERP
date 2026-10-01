using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Entities
{
    public class AiSearchLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    }
}
