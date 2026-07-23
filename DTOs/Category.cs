namespace Backend.DTOs;

public class CategoryResponse
{
    public int Id { get; set; }
    public int CanteenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
