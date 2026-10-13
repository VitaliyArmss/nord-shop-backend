namespace NordShop.Api.Models;

public sealed class DemoUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public required string Role { get; set; }
}
