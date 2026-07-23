namespace Backend.DTOs;

public class FavoriteResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal BasePriceAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
