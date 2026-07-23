namespace Backend.Models;

public class Category
{
    public int Id { get; set; }

    public int CanteenId { get; set; }

    public Canteen Canteen { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public List<Product> Products { get; set; } = new();
}
