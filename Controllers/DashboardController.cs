using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Helpers;

namespace Backend.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public DashboardController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet("revenue")]
    public ActionResult<List<RevenueItemResponse>> GetRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var query = _db.Orders
            .Include(o => o.Canteen)
            .Where(o => o.Status == OrderStatus.Delivered)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc));
        if (to.HasValue)
            query = query.Where(o => o.CreatedAt < DateTime.SpecifyKind(to.Value.Date, DateTimeKind.Utc).AddDays(1));

        var revenue = query
            .GroupBy(o => new { o.CanteenId, o.Canteen.Name })
            .Select(g => new RevenueItemResponse
            {
                CanteenId = g.Key.CanteenId,
                CanteenName = g.Key.Name,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(r => r.TotalRevenue)
            .ToList();

        return revenue;
    }

    [HttpGet("canteens/{canteenId}/stats")]
    public ActionResult<CanteenStatsResponse> GetStats(int canteenId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căn tin.");

        var orders = _db.Orders.Where(o => o.CanteenId == canteenId);

        return new CanteenStatsResponse
        {
            TotalOrders = orders.Count(),
            TotalRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
            TotalProducts = _db.Products.Count(p => p.CanteenId == canteenId),
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            PreparingOrders = orders.Count(o => o.Status == OrderStatus.Preparing),
            ReadyForPickupOrders = orders.Count(o => o.Status == OrderStatus.ReadyForPickup),
            DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled)
        };
    }

    [HttpGet("canteens/{canteenId}/orders/recent")]
    public ActionResult<List<OrderResponse>> GetRecentOrders(int canteenId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căn tin.");

        var list = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CanteenId == canteenId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .Select(o => new OrderResponse
            {
                Id = o.Id,
                UserId = o.UserId,
                CanteenId = o.CanteenId,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                OrderType = o.OrderType,
                IsAsap = o.IsAsap,
                PickupTime = o.PickupTime,
                Note = o.Note,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItemResponse
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Note = i.Note,
                    SelectedModifiersJson = i.SelectedModifiersJson,
                    SubTotal = i.SubTotal
                }).ToList()
            }).ToList();

        return list;
    }

    [HttpGet("canteens/{canteenId}/products/top")]
    public ActionResult<List<ProductResponse>> GetTopProducts(int canteenId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căn tin.");

        var list = _db.Products
            .Include(p => p.Category)
            .Where(p => p.CanteenId == canteenId)
            .OrderByDescending(p => p.SoldCount)
            .Take(10)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                CanteenId = p.CanteenId,
                Name = p.Name,
                Description = p.Description,
                BasePriceAmount = p.BasePriceAmount,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                SoldCount = p.SoldCount,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name
            }).ToList();

        return list;
    }
}
