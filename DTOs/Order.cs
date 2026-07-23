using Backend.Enums;

namespace Backend.DTOs;

public class OrderResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CanteenId { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public OrderType OrderType { get; set; }
    public bool IsAsap { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class OrderItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string? SelectedModifiersJson { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreateOrderRequest
{
    public int CanteenId { get; set; }
    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public bool IsAsap { get; set; } = true;
    public TimeOnly? PickupTime { get; set; }
    public string? Note { get; set; }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}

public class UpdatePaymentStatusRequest
{
    public PaymentStatus PaymentStatus { get; set; }
}
