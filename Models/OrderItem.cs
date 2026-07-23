namespace Backend.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string? SelectedModifiersJson { get; set; }

    public decimal SubTotal { get; set; }
    public Order Order { get; set; } = null!;
}
