using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Helpers;

namespace Backend.Controllers;

[ApiController]
public class ModifierGroupController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public ModifierGroupController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet("api/products/{productId}/modifier-groups")]
    public ActionResult<List<ModifierGroupResponse>> GetAll(int productId)
    {
        var product = _db.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        var list = _db.ModifierGroups
            .Where(mg => mg.ProductId == productId)
            .Include(mg => mg.Modifiers.OrderBy(m => m.DisplayOrder))
            .OrderBy(mg => mg.DisplayOrder)
            .Select(mg => new ModifierGroupResponse
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
            }).ToList();

        return list;
    }

    [HttpPost("api/products/{productId}/modifier-groups")]
    public ActionResult<ModifierGroupResponse> Create(int productId, CreateModifierGroupRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var product = _db.Products.FirstOrDefault(p => p.Id == productId);
        if (product == null) return NotFound("Không tìm thấy sản phẩm.");

        var group = new ModifierGroup
        {
            ProductId = productId,
            Name = request.Name,
            Required = request.Required,
            MaxSelected = request.MaxSelected,
            DisplayOrder = request.DisplayOrder
        };

        _db.ModifierGroups.Add(group);
        _db.SaveChanges();

        return new ModifierGroupResponse
        {
            Id = group.Id,
            ProductId = group.ProductId,
            Name = group.Name,
            Required = group.Required,
            MaxSelected = group.MaxSelected,
            DisplayOrder = group.DisplayOrder,
            Status = group.Status
        };
    }

    [HttpPut("api/modifier-groups/{id}")]
    public ActionResult Update(int id, UpdateModifierGroupRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var group = _db.ModifierGroups.FirstOrDefault(mg => mg.Id == id);
        if (group == null) return NotFound("Không tìm thấy nhóm modifier.");

        if (request.Name != null) group.Name = request.Name;
        if (request.Required.HasValue) group.Required = request.Required.Value;
        if (request.MaxSelected.HasValue) group.MaxSelected = request.MaxSelected.Value;
        if (request.DisplayOrder.HasValue) group.DisplayOrder = request.DisplayOrder.Value;

        _db.SaveChanges();
        return Ok("Cập nhật nhóm modifier thành công.");
    }

    [HttpPatch("api/modifier-groups/{id}/status")]
    public ActionResult UpdateStatus(int id, UpdateModifierGroupStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var group = _db.ModifierGroups.FirstOrDefault(mg => mg.Id == id);
        if (group == null) return NotFound("Không tìm thấy nhóm modifier.");

        group.Status = request.Status;
        _db.SaveChanges();
        return Ok("Cập nhật trạng thái nhóm modifier thành công.");
    }

    [HttpDelete("api/modifier-groups/{id}")]
    public ActionResult Delete(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var group = _db.ModifierGroups
            .Include(mg => mg.Modifiers)
            .FirstOrDefault(mg => mg.Id == id);

        if (group == null) return NotFound("Không tìm thấy nhóm modifier.");

        _db.Modifiers.RemoveRange(group.Modifiers);
        _db.ModifierGroups.Remove(group);
        _db.SaveChanges();

        return Ok("Xoá nhóm modifier thành công.");
    }
}
