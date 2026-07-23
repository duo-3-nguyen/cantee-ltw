using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public OrderController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpPost]
    public ActionResult<OrderResponse> Create(CreateOrderRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var cart = _db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == user.Id && c.CanteenId == request.CanteenId);

        if (cart == null || cart.Items.Count == 0)
            return BadRequest("Giỏ hàng trống. Vui lòng thêm món trước khi tạo đơn.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == request.CanteenId);
        if (canteen == null) return NotFound("Không tìm thấy căng tin.");
        if (canteen.Status != CanteenStatus.Active)
            return BadRequest("Căng tin hiện không hoạt động.");

        var order = new Order
        {
            UserId = user.Id,
            CanteenId = request.CanteenId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
            OrderType = request.OrderType,
            IsAsap = request.IsAsap,
            PickupTime = request.PickupTime,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        decimal total = 0;
        foreach (var cartItem in cart.Items)
        {
            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                UnitPrice = cartItem.UnitPrice,
                Quantity = cartItem.Quantity,
                Note = cartItem.Note,
                SelectedModifiersJson = cartItem.SelectedModifiersJson,
                SubTotal = cartItem.UnitPrice * cartItem.Quantity
            };
            total += orderItem.SubTotal;
            order.Items.Add(orderItem);
        }

        order.TotalAmount = total;
        _db.Orders.Add(order);

        _db.CartItems.RemoveRange(cart.Items);
        _db.SaveChanges();

        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            CanteenId = order.CanteenId,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            OrderType = order.OrderType,
            IsAsap = order.IsAsap,
            PickupTime = order.PickupTime,
            Note = order.Note,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponse
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
        };
    }

    [HttpGet]
    public ActionResult<List<OrderResponse>> GetMyOrders()
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var list = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
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

    [HttpGet("{id}")]
    public ActionResult<OrderResponse> GetById(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var order = _db.Orders
            .Include(o => o.Items)
            .FirstOrDefault(o => o.Id == id);

        if (order == null) return NotFound("Không tìm thấy đơn hàng.");

        if (user.Role == UserRole.Customer && order.UserId != user.Id)
            return StatusCode(403, "Bạn không có quyền xem đơn hàng này.");

        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            CanteenId = order.CanteenId,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            OrderType = order.OrderType,
            IsAsap = order.IsAsap,
            PickupTime = order.PickupTime,
            Note = order.Note,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponse
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
        };
    }

    [HttpGet("~/api/canteens/{canteenId}/orders")]
    public ActionResult<List<OrderResponse>> GetByCanteen(
        int canteenId,
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? date)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var query = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CanteenId == canteenId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        if (date.HasValue)
        {
            var from = date.Value.Date;
            var to = from.AddDays(1);
            query = query.Where(o => o.CreatedAt >= from && o.CreatedAt < to);
        }

        var list = query.OrderByDescending(o => o.CreatedAt)
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

    [HttpPatch("{id}/status")]
    public ActionResult UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var order = _db.Orders.FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound("Không tìm thấy đơn hàng.");

        order.Status = request.Status;

        if (request.Status == OrderStatus.Delivered)
            UpdateProductSoldCount(order);

        _db.SaveChanges();
        return Ok("Cập nhật trạng thái đơn hàng thành công.");
    }

    [HttpPatch("{id}/payment")]
    public ActionResult UpdatePayment(int id, UpdatePaymentStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var order = _db.Orders.FirstOrDefault(o => o.Id == id);
        if (order == null) return NotFound("Không tìm thấy đơn hàng.");

        order.PaymentStatus = request.PaymentStatus;
        _db.SaveChanges();
        return Ok("Cập nhật trạng thái thanh toán thành công.");
    }

    private void UpdateProductSoldCount(Order order)
    {
        var orderItems = _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToList();
        foreach (var item in orderItems)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
                product.SoldCount += item.Quantity;
        }
    }
}
