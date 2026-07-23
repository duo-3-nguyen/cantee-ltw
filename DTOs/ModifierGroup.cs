using Backend.Enums;

namespace Backend.DTOs;

public class ModifierGroupResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public int MaxSelected { get; set; }
    public int DisplayOrder { get; set; }
    public StockStatus Status { get; set; }
    public List<ModifierResponse> Modifiers { get; set; } = new();
}

public class CreateModifierGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public int MaxSelected { get; set; } = 1;
    public int DisplayOrder { get; set; }
}

public class UpdateModifierGroupRequest
{
    public string? Name { get; set; }
    public bool? Required { get; set; }
    public int? MaxSelected { get; set; }
    public int? DisplayOrder { get; set; }
}

public class UpdateModifierGroupStatusRequest
{
    public StockStatus Status { get; set; }
}
