using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        public InvoiceService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<InvoiceResponse> GenerateAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException($"Order {orderId} not found.");

            if (order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Cannot generate an invoice for a cancelled order.");


            var existingInvoices = await _unitOfWork.Invoices.GetAllAsync();
            var existing = existingInvoices.FirstOrDefault(i => i.OrderId == orderId);
            if (existing != null)
                throw new InvalidOperationException($"An invoice ({existing.InvoiceNumber}) already exists for this order.");

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{orderId:D4}",
                OrderId = orderId
            };

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            return new InvoiceResponse { Id = invoice.Id, InvoiceNumber = invoice.InvoiceNumber, IssuedDate = invoice.IssuedDate, OrderId = invoice.OrderId };
        }

        public async Task<IEnumerable<InvoiceResponse>> GetAllAsync()
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            return invoices.Select(i => new InvoiceResponse { Id = i.Id, InvoiceNumber = i.InvoiceNumber, IssuedDate = i.IssuedDate, OrderId = i.OrderId });
        }

        public async Task<byte[]> GeneratePdfAsync(int invoiceId)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

            var order = await _unitOfWork.Orders.GetByIdWithIncludesAsync(invoice.OrderId, o => o.Customer, o => o.Items)
                ?? throw new KeyNotFoundException("Related order not found.");

            // Pehle sab product names fetch kar lete hain (PDF banane se PEHLE, async yahan theek hai)
            var productNames = new Dictionary<int, string>();
            foreach (var item in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                productNames[item.ProductId] = product?.Name ?? "Unknown";
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("MiniERP").FontSize(22).Bold().FontColor("#1F3864");
                        col.Item().Text($"Invoice {invoice.InvoiceNumber}").FontSize(14).FontColor("#6B7280");
                        col.Item().Text($"Date: {invoice.IssuedDate:yyyy-MM-dd}").FontSize(11);
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text($"Bill To: {order.Customer?.Name}").Bold();
                        col.Item().Text(order.Customer?.Email ?? "");
                        col.Item().PaddingTop(16).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Product").Bold();
                                header.Cell().Text("Qty").Bold();
                                header.Cell().Text("Price").Bold();
                                header.Cell().Text("Subtotal").Bold();
                            });

                            // Ab yahan koi async call nahi, sirf dictionary se lookup
                            foreach (var item in order.Items)
                            {
                                table.Cell().Text(productNames.GetValueOrDefault(item.ProductId, "Unknown"));
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text($"Rs. {item.UnitPrice:N0}");
                                table.Cell().Text($"Rs. {item.Quantity * item.UnitPrice:N0}");
                            }
                        });

                        col.Item().PaddingTop(16).AlignRight().Text($"Total: Rs. {order.TotalAmount:N0}").FontSize(14).Bold();
                    });

                    page.Footer().AlignCenter().Text("Thank you for your business — MiniERP").FontSize(10).FontColor("#6B7280");
                });
            });

            return document.GeneratePdf();
        }
    }
}
