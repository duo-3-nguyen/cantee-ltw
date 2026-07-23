using Backend.Enums;

namespace Backend.Models;

public class ModifierGroup
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public int MaxSelected { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public StockStatus Status { get; set; } = StockStatus.Available;

    public Product Product { get; set; } = null!;
    public List<Modifier> Modifiers { get; set; } = new();
}
