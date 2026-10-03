using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        public PurchaseOrderService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }
        public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, string userId, string userEmail)
        {
            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Purchase order must contain at least one item.");

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Quantity must be greater than zero.");
                if (item.UnitCost <= 0)
                    throw new ArgumentException("Unit cost must be greater than zero.");
            }

            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId)
                ?? throw new KeyNotFoundException($"Supplier {request.SupplierId} not found.");

            var po = new PurchaseOrder
            {
                SupplierId = request.SupplierId,
                Status = PurchaseOrderStatus.Pending,
                Items = request.Items.Select(i => new PurchaseOrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost
                }).ToList()
            };

            await _unitOfWork.PurchaseOrders.AddAsync(po);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(userId, userEmail, "PurchaseOrderCreated", "PurchaseOrder", po.Id.ToString(),
    $"Purchase Order #{po.Id} created from {supplier.Name}, Total: Rs.{po.TotalAmount}");
            return await BuildResponse(po.Id);
        }

        public async Task<IEnumerable<PurchaseOrderResponse>> GetAllAsync()
        {
            var orders = await _unitOfWork.PurchaseOrders.GetAllWithIncludesAsync(po => po.Supplier, po => po.Items);
            var result = new List<PurchaseOrderResponse>();
            foreach (var po in orders)
                result.Add(await MapToResponse(po));
            return result;
        }

        public async Task MarkAsReceivedAsync(int purchaseOrderId, string userId, string userEmail)
        {
            var po = await _unitOfWork.PurchaseOrders.GetByIdWithIncludesAsync(purchaseOrderId, p => p.Items)
                ?? throw new KeyNotFoundException($"Purchase order {purchaseOrderId} not found.");

            if (po.Status == PurchaseOrderStatus.Received)
                throw new InvalidOperationException("This purchase order is already marked as received.");

            // Stock badhate hain — yeh Order ke ulta logic hai
            foreach (var item in po.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    _unitOfWork.Products.Update(product);
                }
            }

            po.Status = PurchaseOrderStatus.Received;
            _unitOfWork.PurchaseOrders.Update(po);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(userId, userEmail, "PurchaseOrderReceived", "PurchaseOrder", purchaseOrderId.ToString(),
    $"Purchase Order #{purchaseOrderId} marked as received, stock updated");
        }

        private async Task<PurchaseOrderResponse> BuildResponse(int id)
        {
            var po = await _unitOfWork.PurchaseOrders.GetByIdWithIncludesAsync(id, p => p.Supplier, p => p.Items);
            return await MapToResponse(po!);
        }

        private async Task<PurchaseOrderResponse> MapToResponse(PurchaseOrder po)
        {
            var response = new PurchaseOrderResponse
            {
                Id = po.Id,
                OrderDate = po.OrderDate,
                Status = po.Status.ToString(),
                SupplierName = po.Supplier?.Name ?? "Unknown",
                TotalAmount = po.TotalAmount
            };

            foreach (var item in po.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                response.Items.Add(new PurchaseOrderItemResponse
                {
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost
                });
            }

            return response;
        }
    }
}
