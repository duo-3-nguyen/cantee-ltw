using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Helpers;

namespace Backend.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public ProductController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet]
    public ActionResult<List<ProductResponse>> GetAll(
        [FromQuery] int? canteenId,
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] StockStatus? status)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (canteenId.HasValue)
            query = query.Where(p => p.CanteenId == canteenId.Value);
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var list = query.OrderBy(p => p.Name).Select(p => new ProductResponse
        {
            Id = p.Id,
            CanteenId = p.CanteenId,
            Name = p.Name,
            Description = p.Description,
            BasePriceAmount = p.BasePriceAmount,
            ImageUrl = p.ImageUrl,
            Status = p.Status,
            SoldCount = p.SoldCount,
            FavoriteCount = p.Favorites.Count,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name
        }).ToList();

        return list;
    }

    [HttpGet("{id}")]
    public ActionResult<ProductDetailResponse> GetById(int id)
    {
        var product = _db.Products
            .Include(p => p.Category)
            .Include(p => p.ModifierGroups.OrderBy(mg => mg.DisplayOrder))
                .ThenInclude(mg => mg.Modifiers.OrderBy(m => m.DisplayOrder))
            .FirstOrDefault(p => p.Id == id);

        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        return new ProductDetailResponse
        {
            Id = product.Id,
            CanteenId = product.CanteenId,
            Name = product.Name,
            Description = product.Description,
            BasePriceAmount = product.BasePriceAmount,
            ImageUrl = product.ImageUrl,
            Status = product.Status,
            SoldCount = product.SoldCount,
            FavoriteCount = product.Favorites.Count,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            ModifierGroups = product.ModifierGroups.Select(mg => new ModifierGroupResponse
            {
                Id = mg.Id,
                ProductId = mg.ProductId,
                Name = mg.Name,
                Required = mg.Required,
                MaxSelected = mg.MaxSelected,
                DisplayOrder = mg.DisplayOrder,
                Status = mg.Status,
                Modifiers = mg.Modifiers.Select(m => new ModifierResponse
                {
                    Id = m.Id,
                    ModifierGroupId = m.ModifierGroupId,
                    Name = m.Name,
                    PriceAmount = m.PriceAmount,
                    Status = m.Status,
                    DisplayOrder = m.DisplayOrder,
                    IsDefault = m.IsDefault
                }).ToList()
            }).ToList()
        };
    }

    [HttpPost("~/api/canteens/{canteenId}/products")]
    public ActionResult<ProductResponse> Create(int canteenId, [FromForm] CreateProductRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căn tin.");

        var category = _db.Categories.FirstOrDefault(c => c.Id == request.CategoryId);
        if (category == null) return NotFound("Không tìm thấy danh mục.");

        var product = new Product
        {
            CanteenId = canteenId,
            Name = request.Name,
            Description = request.Description,
            BasePriceAmount = request.BasePriceAmount,
            ImageUrl = SaveImage(request.Image, "products"),
            Status = request.Status,
            CategoryId = request.CategoryId
        };

        _db.Products.Add(product);
        _db.SaveChanges();

        return new ProductResponse
        {
            Id = product.Id,
            CanteenId = product.CanteenId,
            Name = product.Name,
            Description = product.Description,
            BasePriceAmount = product.BasePriceAmount,
            ImageUrl = product.ImageUrl,
            Status = product.Status,
            SoldCount = product.SoldCount,
            FavoriteCount = 0,
            CategoryId = product.CategoryId,
            CategoryName = category.Name
        };
    }

    [HttpPost("{id}/image")]
    public ActionResult UpdateImage(int id, IFormFile image)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        product.ImageUrl = SaveImage(image, "products");
        _db.SaveChanges();
        return Ok("Cập nhật ảnh sản phẩm thành công.");
    }

    [HttpPut("{id}")]
    public ActionResult Update(int id, UpdateProductRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.BasePriceAmount.HasValue) product.BasePriceAmount = request.BasePriceAmount.Value;
        if (request.CategoryId.HasValue)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == request.CategoryId.Value);
            if (category == null) return NotFound("Không tìm thấy danh mục.");
            product.CategoryId = request.CategoryId.Value;
        }
        if (request.Status.HasValue) product.Status = request.Status.Value;

        _db.SaveChanges();
        return Ok("Cập nhật sản phẩm thành công.");
    }

    [HttpPatch("{id}/status")]
    public ActionResult UpdateStatus(int id, UpdateProductStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        product.Status = request.Status;
        _db.SaveChanges();
        return Ok("Cập nhật trạng thái sản phẩm thành công.");
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products
            .Include(p => p.ModifierGroups)
                .ThenInclude(mg => mg.Modifiers)
            .FirstOrDefault(p => p.Id == id);

        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        foreach (var mg in product.ModifierGroups)
            _db.Modifiers.RemoveRange(mg.Modifiers);
        _db.ModifierGroups.RemoveRange(product.ModifierGroups);
        _db.Products.Remove(product);
        _db.SaveChanges();

        return Ok("Xoá sản phẩm thành công.");
    }

    private string? SaveImage(IFormFile? image, string folder)
    {
        if (image == null) return null;

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        image.CopyTo(stream);

        return $"/uploads/{folder}/{fileName}";
    }
}
