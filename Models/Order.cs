using Backend.Enums;

namespace Backend.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CanteenId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public bool IsAsap { get; set; } = true;
    public TimeOnly? PickupTime { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    

    public User User { get; set; } = null!;
    public Canteen Canteen { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}
