using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.DTOs
{
    public class SmartSearchRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    public class SmartSearchResponse
    {
        public List<ProductResponse> Products { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
    }
}
