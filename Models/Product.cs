using Backend.Enums;

namespace Backend.Models;

public class Product
{
    public int Id { get; set; }
    public int CanteenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePriceAmount { get; set; }
    public string? ImageUrl { get; set; }
    public StockStatus Status { get; set; } = StockStatus.Available;
    public int SoldCount { get; set; }
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;
    public Canteen Canteen { get; set; } = null!;
    public List<ModifierGroup> ModifierGroups { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}
