using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
public class ModifierController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public ModifierController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet("api/modifier-groups/{groupId}/modifiers")]
    public ActionResult<List<ModifierResponse>> GetAll(int groupId)
    {
        var group = _db.ModifierGroups.FirstOrDefault(mg => mg.Id == groupId);
        if (group == null) return NotFound("Không tìm thấy nhóm modifier.");

        var list = _db.Modifiers
            .Where(m => m.ModifierGroupId == groupId)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new ModifierResponse
            {
                Id = m.Id,
                ModifierGroupId = m.ModifierGroupId,
                Name = m.Name,
                PriceAmount = m.PriceAmount,
                Status = m.Status,
                DisplayOrder = m.DisplayOrder,
                IsDefault = m.IsDefault
            }).ToList();

        return list;
    }

    [HttpPost("api/modifier-groups/{groupId}/modifiers")]
    public ActionResult<ModifierResponse> Create(int groupId, CreateModifierRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var group = _db.ModifierGroups.FirstOrDefault(mg => mg.Id == groupId);
        if (group == null) return NotFound("Không tìm thấy nhóm modifier.");

        var modifier = new Modifier
        {
            ModifierGroupId = groupId,
            Name = request.Name,
            PriceAmount = request.PriceAmount,
            DisplayOrder = request.DisplayOrder,
            IsDefault = request.IsDefault
        };

        _db.Modifiers.Add(modifier);
        _db.SaveChanges();

        return new ModifierResponse
        {
            Id = modifier.Id,
            ModifierGroupId = modifier.ModifierGroupId,
            Name = modifier.Name,
            PriceAmount = modifier.PriceAmount,
            Status = modifier.Status,
            DisplayOrder = modifier.DisplayOrder,
            IsDefault = modifier.IsDefault
        };
    }

    [HttpPut("api/modifiers/{id}")]
    public ActionResult Update(int id, UpdateModifierRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var modifier = _db.Modifiers.FirstOrDefault(m => m.Id == id);
        if (modifier == null) return NotFound("Không tìm thấy modifier.");

        if (request.Name != null) modifier.Name = request.Name;
        if (request.PriceAmount.HasValue) modifier.PriceAmount = request.PriceAmount.Value;
        if (request.DisplayOrder.HasValue) modifier.DisplayOrder = request.DisplayOrder.Value;
        if (request.IsDefault.HasValue) modifier.IsDefault = request.IsDefault.Value;

        _db.SaveChanges();
        return Ok("Cập nhật modifier thành công.");
    }

    [HttpPatch("api/modifiers/{id}/status")]
    public ActionResult UpdateStatus(int id, UpdateModifierStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var modifier = _db.Modifiers.FirstOrDefault(m => m.Id == id);
        if (modifier == null) return NotFound("Không tìm thấy modifier.");

        modifier.Status = request.Status;
        _db.SaveChanges();
        return Ok("Cập nhật trạng thái modifier thành công.");
    }

    [HttpDelete("api/modifiers/{id}")]
    public ActionResult Delete(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var modifier = _db.Modifiers.FirstOrDefault(m => m.Id == id);
        if (modifier == null) return NotFound("Không tìm thấy modifier.");

        _db.Modifiers.Remove(modifier);
        _db.SaveChanges();

        return Ok("Xoá modifier thành công.");
    }
}
