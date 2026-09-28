using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MonthlySalesResponse>> GetSalesByMonthAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllWithIncludesAsync(o => o.Items);

            // Cancelled orders ko sales figures mein count nahi karte — yeh important business rule hai
            var validOrders = orders.Where(o => o.Status != OrderStatus.Cancelled);

            var grouped = validOrders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlySalesResponse
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToList();

            return grouped;
        }

        public async Task<IEnumerable<TopProductResponse>> GetTopProductsAsync(int topN = 5)
        {
            var orders = await _unitOfWork.Orders.GetAllWithIncludesAsync(o => o.Items);
            var validOrders = orders.Where(o => o.Status != OrderStatus.Cancelled);

            var itemGroups = validOrders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantitySold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(topN)
                .ToList();

            var result = new List<TopProductResponse>();
            foreach (var item in itemGroups)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                result.Add(new TopProductResponse
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    TotalQuantitySold = item.TotalQuantitySold,
                    TotalRevenue = item.TotalRevenue
                });
            }

            return result;
        }
    }
}
