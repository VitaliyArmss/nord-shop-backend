namespace NordShop.Api.Models;

public sealed class ShopOrder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DemoUser? User { get; set; }
    public required string DeliveryAddress { get; set; }
    public string? Comment { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }
    public List<ShopOrderItem> Items { get; set; } = [];
}
