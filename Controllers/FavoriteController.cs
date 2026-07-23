using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/favorites")]
public class FavoriteController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public FavoriteController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet]
    public ActionResult<List<FavoriteResponse>> GetAll()
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var list = _db.Favorites
            .Include(f => f.Product)
            .Where(f => f.UserId == user.Id)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FavoriteResponse
            {
                Id = f.Id,
                ProductId = f.ProductId,
                ProductName = f.Product.Name,
                ImageUrl = f.Product.ImageUrl,
                BasePriceAmount = f.Product.BasePriceAmount,
                CreatedAt = f.CreatedAt
            }).ToList();

        return list;
    }

    [HttpPost("{productId}")]
    public ActionResult Add(int productId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        var existing = _db.Favorites
            .FirstOrDefault(f => f.UserId == user.Id && f.ProductId == productId);
        if (existing != null)
            return Conflict("Sản phẩm đã có trong danh sách yêu thích.");

        var favorite = new Favorite
        {
            UserId = user.Id,
            ProductId = productId
        };

        _db.Favorites.Add(favorite);
        _db.SaveChanges();

        return Ok("Đã thêm vào yêu thích.");
    }

    [HttpDelete("{productId}")]
    public ActionResult Remove(int productId)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Customer))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var favorite = _db.Favorites
            .FirstOrDefault(f => f.UserId == user.Id && f.ProductId == productId);

        if (favorite == null) return NotFound("Không tìm thấy sản phẩm yêu thích.");

        _db.Favorites.Remove(favorite);
        _db.SaveChanges();

        return Ok("Đã bỏ yêu thích.");
    }
}
