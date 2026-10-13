using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NordShop.Api.Data;
using NordShop.Api.Models;

namespace NordShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class OrdersController(ShopDbContext db) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var orders = await db.Orders.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(orders.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var quantities = request.Items
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Quantity));

        if (quantities.Values.Any(quantity => quantity > 10))
            return BadRequest(new { message = "Для одного товара можно заказать не больше 10 штук." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var products = await db.Products
            .Where(x => quantities.Keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != quantities.Count)
            return BadRequest(new { message = "Некоторые товары больше недоступны." });

        foreach (var (productId, quantity) in quantities.OrderBy(x => x.Key))
        {
            var product = products[productId];
            // A conditional SQL update locks the row and prevents two concurrent orders from
            // overselling the same remaining stock. The enclosing transaction rolls back on failure.
            var updated = await db.Products
                .Where(x => x.Id == productId && x.Stock >= quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Stock, x => x.Stock - quantity), cancellationToken);

            if (updated != 1)
                return BadRequest(new { message = $"Недостаточно товара «{product.Name}» на складе." });
        }

        var order = new ShopOrder
        {
            UserId = userId,
            DeliveryAddress = request.DeliveryAddress.Trim(),
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            Status = "Принят",
            CreatedAtUtc = DateTime.UtcNow,
            Items = quantities.Select(line => new ShopOrderItem
            {
                ProductId = line.Key,
                Quantity = line.Value,
                UnitPrice = products[line.Key].Price
            }).ToList()
        };
        order.Total = order.Items.Sum(item => item.Quantity * item.UnitPrice);

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var item in order.Items)
            item.Product = products[item.ProductId];

        return Ok(ToResponse(order));
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static OrderResponse ToResponse(ShopOrder order) => new(
        order.Id,
        order.CreatedAtUtc,
        order.Status,
        order.DeliveryAddress,
        order.Total,
        order.Items.Select(item => new OrderItemResponse(
            item.ProductId,
            item.Product?.Name ?? "Товар",
            item.Product?.ImageUrl ?? string.Empty,
            item.Quantity,
            item.UnitPrice)).ToList());
}
