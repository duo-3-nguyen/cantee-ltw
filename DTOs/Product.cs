using Microsoft.AspNetCore.Http;
using Backend.Enums;

namespace Backend.DTOs;

public class ProductResponse
{
    public int Id { get; set; }
    public int CanteenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePriceAmount { get; set; }
    public string? ImageUrl { get; set; }
    public StockStatus Status { get; set; }
    public int SoldCount { get; set; }
    public int FavoriteCount { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class ProductDetailResponse : ProductResponse
{
    public List<ModifierGroupResponse> ModifierGroups { get; set; } = new();
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePriceAmount { get; set; }
    public int CategoryId { get; set; }
    public StockStatus Status { get; set; } = StockStatus.Available;
    public IFormFile? Image { get; set; }
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? BasePriceAmount { get; set; }
    public int? CategoryId { get; set; }
    public StockStatus? Status { get; set; }
}

public class UpdateProductStatusRequest
{
    public StockStatus Status { get; set; }
}
