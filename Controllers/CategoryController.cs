using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Helpers;

namespace Backend.Controllers;

[ApiController]
public class CategoryController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public CategoryController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet("api/canteens/{canteenId}/categories")]
    public ActionResult<List<CategoryResponse>> GetAll(int canteenId)
    {
        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căn tin.");

        var list = _db.Categories
            .Where(c => c.CanteenId == canteenId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                CanteenId = c.CanteenId,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder
            }).ToList();

        return list;
    }

    [HttpPost("api/canteens/{canteenId}/categories")]
    public ActionResult<CategoryResponse> Create(int canteenId, CreateCategoryRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căn tin.");

        var category = new Category
        {
            CanteenId = canteenId,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        };

        _db.Categories.Add(category);
        _db.SaveChanges();

        return new CategoryResponse
        {
            Id = category.Id,
            CanteenId = category.CanteenId,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder
        };
    }

    [HttpPut("api/categories/{id}")]
    public ActionResult Update(int id, UpdateCategoryRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var category = _db.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null) return NotFound("Không tìm thấy danh mục.");

        category.Name = request.Name;
        category.DisplayOrder = request.DisplayOrder;
        _db.SaveChanges();

        return Ok("Cập nhật danh mục thành công.");
    }

    [HttpDelete("api/categories/{id}")]
    public ActionResult Delete(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var category = _db.Categories.Include(c => c.Products).FirstOrDefault(c => c.Id == id);
        if (category == null) return NotFound("Không tìm thấy danh mục.");

        if (category.Products.Count > 0)
            return BadRequest("Danh mục đang có sản phẩm. Vui lòng xử lý trước khi xoá.");

        _db.Categories.Remove(category);
        _db.SaveChanges();

        return Ok("Xoá danh mục thành công.");
    }
}
