using MiniERP.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponse> GenerateAsync(int orderId);
        Task<IEnumerable<InvoiceResponse>> GetAllAsync();
        Task<byte[]> GeneratePdfAsync(int invoiceId);
    }
}
