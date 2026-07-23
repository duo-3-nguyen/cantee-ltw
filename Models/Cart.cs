namespace Backend.Models;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CanteenId { get; set; }

    public Canteen Canteen { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<CartItem> Items { get; set; } = new();
}
