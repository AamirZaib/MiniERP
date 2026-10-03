using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IGlobalSearchService
    {
        Task<GlobalSearchResponse> SearchAsync(string query);
    }
}
