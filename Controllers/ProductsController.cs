using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NordShop.Api.Data;
using NordShop.Api.Models;

namespace NordShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await db.Products.AsNoTracking()
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return product is null ? NotFound(new { message = "Товар не найден." }) : Ok(product);
    }
}
