using Portfolio.TradeOrderService.DataAccess;
using Portfolio.TradeOrderService.Models;
using Portfolio.TradeOrderService.Models.Requests;
using Portfolio.TradeOrderService.Models.Responses;
using Portfolio.TradeOrderService.ServiceInterfaces;

namespace Portfolio.TradeOrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string idempotencyKey)
    {
        // Idempotency check — return existing order if key already seen
        var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey);
        if (existing is not null)
            return MapResponse(existing);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            AccountId = request.AccountId,
            Symbol = request.Symbol.ToUpperInvariant(),
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = OrderStatus.PENDING,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var created = await _repository.CreateAsync(order);
        return MapResponse(created);
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        return order is null ? null : MapResponse(order);
    }

    public async Task<PagedResponse<OrderResponse>> ListOrdersAsync(
        string? accountId, string? symbol, OrderStatus? status, int page, int pageSize)
    {
        var (items, totalCount) = await _repository.ListAsync(accountId, symbol, status, page, pageSize);
        return new PagedResponse<OrderResponse>
        {
            Items = items.Select(MapResponse),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static OrderResponse MapResponse(Order o) => new()
    {
        Id = o.Id,
        IdempotencyKey = o.IdempotencyKey,
        AccountId = o.AccountId,
        Symbol = o.Symbol,
        Side = o.Side.ToString(),
        Quantity = o.Quantity,
        Price = o.Price,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };
}
