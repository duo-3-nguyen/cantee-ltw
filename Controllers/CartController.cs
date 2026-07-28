using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Helpers;

namespace Backend.Controllers;

[ApiController]
[Route("api/canteens/{canteenId}/cart")]
public class CartController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public CartController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet]
    public ActionResult<CartResponse> GetCart(int canteenId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var cart = _db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == user.Id && c.CanteenId == canteenId);

        if (cart == null)
            return new CartResponse { CanteenId = canteenId, Items = new() };

        return new CartResponse
        {
            Id = cart.Id,
            CanteenId = cart.CanteenId,
            Items = cart.Items.Select(i => new CartItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Note = i.Note,
                SelectedModifiersJson = i.SelectedModifiersJson
            }).ToList()
        };
    }

    [HttpPost("items")]
    public ActionResult<CartItemResponse> AddItem(int canteenId, AddCartItemRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products.FirstOrDefault(p => p.Id == request.ProductId);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        var cart = _db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == user.Id && c.CanteenId == canteenId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = user.Id,
                CanteenId = canteenId
            };
            _db.Carts.Add(cart);
            _db.SaveChanges();
        }

        var item = new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.BasePriceAmount,
            Quantity = request.Quantity,
            Note = request.Note,
            SelectedModifiersJson = request.SelectedModifiersJson
        };

        cart.Items.Add(item);
        _db.SaveChanges();

        return new CartItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            Note = item.Note,
            SelectedModifiersJson = item.SelectedModifiersJson
        };
    }

    [HttpPut("items/{id}")]
    public ActionResult UpdateItem(int canteenId, int id, UpdateCartItemRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var cart = _db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == user.Id && c.CanteenId == canteenId);

        if (cart == null) return NotFound("Không tìm thấy giỏ hàng.");

        var item = cart.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound("Không tìm thấy item trong giỏ hàng.");

        if (request.Quantity.HasValue) item.Quantity = request.Quantity.Value;
        if (request.Note != null) item.Note = request.Note;

        _db.SaveChanges();
        return Ok("Cập nhật giỏ hàng thành công.");
    }

    [HttpDelete("items/{id}")]
    public ActionResult DeleteItem(int canteenId, int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var cart = _db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == user.Id && c.CanteenId == canteenId);

        if (cart == null) return NotFound("Không tìm thấy giỏ hàng.");

        var item = cart.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound("Không tìm thấy item trong giỏ hàng.");

        _db.CartItems.Remove(item);
        _db.SaveChanges();
        return Ok("Xoá item khỏi giỏ hàng thành công.");
    }

    [HttpDelete]
    public ActionResult ClearCart(int canteenId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var cart = _db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == user.Id && c.CanteenId == canteenId);

        if (cart == null) return NotFound("Không tìm thấy giỏ hàng.");

        _db.CartItems.RemoveRange(cart.Items);
        _db.SaveChanges();
        return Ok("Xoá toàn bộ giỏ hàng thành công.");
    }
}
