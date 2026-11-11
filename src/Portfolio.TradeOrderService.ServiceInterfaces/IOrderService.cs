using Portfolio.TradeOrderService.Models;
using Portfolio.TradeOrderService.Models.Requests;
using Portfolio.TradeOrderService.Models.Responses;

namespace Portfolio.TradeOrderService.ServiceInterfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string idempotencyKey);
    Task<OrderResponse?> GetOrderAsync(Guid id);
    Task<PagedResponse<OrderResponse>> ListOrdersAsync(string? accountId, string? symbol, OrderStatus? status, int page, int pageSize);
}
