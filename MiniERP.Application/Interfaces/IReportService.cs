using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<MonthlySalesResponse>> GetSalesByMonthAsync();
        Task<IEnumerable<TopProductResponse>> GetTopProductsAsync(int topN = 5);
    }
}
