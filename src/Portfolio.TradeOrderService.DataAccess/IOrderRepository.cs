using Portfolio.TradeOrderService.Models;

namespace Portfolio.TradeOrderService.DataAccess;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<(IEnumerable<Order> Items, int TotalCount)> ListAsync(string? accountId, string? symbol, OrderStatus? status, int page, int pageSize);
    Task<Order> CreateAsync(Order order);
}
