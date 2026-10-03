using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using MiniERP.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public OrderService(IUnitOfWork unitOfWork, IAuditService auditService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _notificationService = notificationService;
        }
        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string userId, string userEmail)
        {

            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Order must contain at least one item.");

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Quantity must be greater than zero.");
            }

            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId)
                ?? throw new KeyNotFoundException($"Customer with Id {request.CustomerId} not found.");

            var order = new Order
            {
                CustomerId = request.CustomerId,
                Status = OrderStatus.Pending,
                Items = new List<OrderItem>()
            };

            // Har product ke liye: stock check karo, order item banao, stock kam karo
            foreach (var itemRequest in request.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemRequest.ProductId)
                    ?? throw new KeyNotFoundException($"Product with Id {itemRequest.ProductId} not found.");

                if (product.StockQuantity < itemRequest.Quantity)
                    throw new InsufficientStockException(product.Name, itemRequest.Quantity, product.StockQuantity);

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price // order ke waqt ka price lock kar dete hain
                });

                // Stock kam karo — yeh business rule hai jo humesha order ke sath honi chahiye
                product.StockQuantity -= itemRequest.Quantity;
                _unitOfWork.Products.Update(product);
                if (product.IsLowStock)
                {
                    await _notificationService.CreateLowStockNotificationAsync(product.Id, product.Name, product.StockQuantity);
                }
            }

            await _unitOfWork.Orders.AddAsync(order);

            // Zaroori: Sab kuch ek hi SaveChangesAsync mein — agar beech mein kuch fail ho, sab rollback ho jayega
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(userId, userEmail, "OrderCreated", "Order", order.Id.ToString(),
        $"Order #{order.Id} created for {customer.Name}, Total: Rs.{order.TotalAmount}");

            return await MapToResponse(order.Id) ?? throw new InvalidOperationException("Order creation failed unexpectedly.");
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int id) => await MapToResponse(id);

        public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllWithIncludesAsync(
                o => o.Customer,
                o => o.Items);

            var responses = new List<OrderResponse>();
            foreach (var order in orders)
            {
                responses.Add(await BuildResponse(order));
            }
            return responses;
        }
        public async Task UpdateOrderStatusAsync(int orderId, string newStatus, string userId, string userEmail)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException($"Order with Id {orderId} not found.");

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var parsedStatus))
                throw new ArgumentException($"'{newStatus}' is not a valid order status.");

            // Business rule: workflow sirf forward direction mein chal sakta hai, aur Cancelled se aage nahi ja sakta
            var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                [OrderStatus.Pending] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
                [OrderStatus.Processing] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
                [OrderStatus.Shipped] = new(), // final state
                [OrderStatus.Cancelled] = new() // final state
            };

            if (!validTransitions[order.Status].Contains(parsedStatus))
                throw new InvalidOrderStatusTransitionException(order.Status.ToString(), parsedStatus.ToString());

            order.Status = parsedStatus;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(userId, userEmail, "OrderStatusChanged", "Order", orderId.ToString(),
            $"Order #{orderId} status changed from {order.Status} to {parsedStatus}");
        }

        private async Task<OrderResponse?> MapToResponse(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdWithIncludesAsync(
                orderId,
                o => o.Customer,
                o => o.Items);

            return order == null ? null : await BuildResponse(order);
        }

        private async Task<OrderResponse> BuildResponse(Order order)
        {
            var response = new OrderResponse
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                CustomerName = order.Customer?.Name ?? "Unknown",
                TotalAmount = order.TotalAmount
            };

            foreach (var item in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                response.Items.Add(new OrderItemResponse
                {
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            return response;
        }
    }
}
