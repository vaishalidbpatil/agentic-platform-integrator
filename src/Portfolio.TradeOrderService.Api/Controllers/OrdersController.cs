using Microsoft.AspNetCore.Mvc;
using Portfolio.TradeOrderService.Models;
using Portfolio.TradeOrderService.Models.Requests;
using Portfolio.TradeOrderService.ServiceInterfaces;

namespace Portfolio.TradeOrderService.Api.Controllers;

[ApiController]
[Route("v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required." });

        var order = await _orderService.CreateOrderAsync(request, idempotencyKey);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> ListOrders(
        [FromQuery] string? accountId,
        [FromQuery] string? symbol,
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _orderService.ListOrdersAsync(accountId, symbol, status, page, pageSize);
        return Ok(result);
    }
}
