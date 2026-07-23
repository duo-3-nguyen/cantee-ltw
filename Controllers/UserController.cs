using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public UserController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet]
    public ActionResult<List<UserDetailResponse>> GetAll()
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var list = _db.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserDetailResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

        return list;
    }

    [HttpGet("{id}")]
    public ActionResult<UserDetailResponse> GetById(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var target = _db.Users.FirstOrDefault(u => u.Id == id);
        if (target == null) return NotFound("Không tìm thấy người dùng.");

        return new UserDetailResponse
        {
            Id = target.Id,
            Username = target.Username,
            Email = target.Email,
            FullName = target.FullName,
            Role = target.Role,
            IsActive = target.IsActive,
            CreatedAt = target.CreatedAt
        };
    }

    [HttpPut("{id}")]
    public ActionResult Update(int id, UpdateUserRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var target = _db.Users.FirstOrDefault(u => u.Id == id);
        if (target == null) return NotFound("Không tìm thấy người dùng.");

        if (request.FullName != null) target.FullName = request.FullName;
        if (request.Email != null) target.Email = request.Email;

        _db.SaveChanges();
        return Ok("Cập nhật người dùng thành công.");
    }

    [HttpPatch("{id}/status")]
    public ActionResult UpdateStatus(int id, UpdateUserStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        if (user.Id == id)
            return BadRequest("Bạn không thể vô hiệu hoá chính mình.");

        var target = _db.Users.FirstOrDefault(u => u.Id == id);
        if (target == null) return NotFound("Không tìm thấy người dùng.");

        target.IsActive = request.IsActive;
        _db.SaveChanges();

        var msg = request.IsActive ? "Kích hoạt người dùng thành công." : "Vô hiệu hoá người dùng thành công.";
        return Ok(msg);
    }
}
