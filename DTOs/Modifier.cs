using Backend.Enums;

namespace Backend.DTOs;

public class ModifierResponse
{
    public int Id { get; set; }
    public int ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public StockStatus Status { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateModifierRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateModifierRequest
{
    public string? Name { get; set; }
    public decimal? PriceAmount { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsDefault { get; set; }
}

public class UpdateModifierStatusRequest
{
    public StockStatus Status { get; set; }
}
