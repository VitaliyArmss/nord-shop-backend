using System.ComponentModel.DataAnnotations;

namespace NordShop.Api.Models;

public sealed class LoginRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserResponse(int Id, string Username, string DisplayName, string Role);
public sealed record LoginResponse(string Token, UserResponse User);

public sealed class CreateOrderRequest
{
    [Required, StringLength(250, MinimumLength = 5)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Comment { get; set; }

    [Required, MinLength(1), MaxLength(30)]
    public List<OrderLineRequest> Items { get; set; } = [];
}

public sealed class OrderLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 10)]
    public int Quantity { get; set; }
}

public sealed record OrderItemResponse(int ProductId, string Name, string ImageUrl, int Quantity, decimal UnitPrice);
public sealed record OrderResponse(int Id, DateTime CreatedAtUtc, string Status, string DeliveryAddress, decimal Total, List<OrderItemResponse> Items);
