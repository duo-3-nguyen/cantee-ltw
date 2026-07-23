namespace Backend.DTOs;

public class RevenueItemResponse
{
    public int CanteenId { get; set; }
    public string CanteenName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class CanteenStatsResponse
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalProducts { get; set; }
    public int PendingOrders { get; set; }
    public int PreparingOrders { get; set; }
    public int ReadyForPickupOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
}
