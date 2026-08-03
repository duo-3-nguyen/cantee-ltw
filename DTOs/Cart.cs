namespace Backend.DTOs;

public class CartResponse
{
    public int Id { get; set; }
    public int CanteenId { get; set; }
    public List<CartItemResponse> Items { get; set; } = new();
}

public class CartItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string? SelectedModifiersJson { get; set; }
}

public class AddCartItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
    public string? SelectedModifiersJson { get; set; }
}

public class UpdateCartItemRequest
{
    public int? Quantity { get; set; }
    public string? Note { get; set; }
}
