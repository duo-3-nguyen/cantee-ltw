namespace Backend.Models;

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
    public string? SelectedModifiersJson { get; set; }

    public Cart Cart { get; set; } = null!;
}
