using System.ComponentModel.DataAnnotations;

namespace Portfolio.TradeOrderService.Models.Requests;

public class CreateOrderRequest
{
    [Required]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public OrderSide Side { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
}
