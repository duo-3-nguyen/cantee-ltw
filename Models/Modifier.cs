using Backend.Enums;

namespace Backend.Models;

public class Modifier
{
    public int Id { get; set; }
    public int ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public StockStatus Status { get; set; } = StockStatus.Available;
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }

    public ModifierGroup ModifierGroup { get; set; } = null!;
}
