using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/canteens")]
public class CanteenController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public CanteenController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet]
    public ActionResult<List<CanteenResponse>> GetAll([FromQuery] CanteenStatus? status, [FromQuery] int? staffId)
    {
        var query = _db.Canteens.Include(c => c.OperatingHours.OrderBy(o => o.DayOfWeek)).AsQueryable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (staffId.HasValue)
            query = query.Where(c => c.StaffId == staffId.Value);

        var list = query.Select(c => new CanteenResponse
        {
            Id = c.Id,
            Name = c.Name,
            Address = c.Address,
            PhoneNumber = c.PhoneNumber,
            Email = c.Email,
            ImageUrl = c.ImageUrl,
            Status = c.Status,
            StaffId = c.StaffId,
            OperatingHours = c.OperatingHours.Select(o => new OperatingHourResponse
            {
                Id = o.Id,
                DayOfWeek = o.DayOfWeek,
                OpenTime = o.OpenTime,
                CloseTime = o.CloseTime,
                IsClosed = o.IsClosed
            }).ToList()
        }).ToList();

        return list;
    }

    [HttpGet("{id}")]
    public ActionResult<CanteenResponse> GetById(int id)
    {
        var canteen = _db.Canteens
            .Include(c => c.OperatingHours.OrderBy(o => o.DayOfWeek))
            .FirstOrDefault(c => c.Id == id);

        if (canteen == null) return NotFound("Không tìm thấy căng tin.");

        return new CanteenResponse
        {
            Id = canteen.Id,
            Name = canteen.Name,
            Address = canteen.Address,
            PhoneNumber = canteen.PhoneNumber,
            Email = canteen.Email,
            ImageUrl = canteen.ImageUrl,
            Status = canteen.Status,
            StaffId = canteen.StaffId,
            OperatingHours = canteen.OperatingHours.Select(o => new OperatingHourResponse
            {
                Id = o.Id,
                DayOfWeek = o.DayOfWeek,
                OpenTime = o.OpenTime,
                CloseTime = o.CloseTime,
                IsClosed = o.IsClosed
            }).ToList()
        };
    }

    [HttpPut("{id}")]
    public ActionResult Update(int id, UpdateCanteenRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == id);
        if (canteen == null) return NotFound("Không tìm thấy căng tin.");

        if (request.Name != null) canteen.Name = request.Name;
        if (request.Address != null) canteen.Address = request.Address;
        if (request.PhoneNumber != null) canteen.PhoneNumber = request.PhoneNumber;
        if (request.Email != null) canteen.Email = request.Email;

        _db.SaveChanges();
        return Ok("Cập nhật căng tin thành công.");
    }

    [HttpPost("{id}/image")]
    public ActionResult UpdateImage(int id, IFormFile image)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == id);
        if (canteen == null) return NotFound("Không tìm thấy căng tin.");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "canteens");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        image.CopyTo(stream);

        canteen.ImageUrl = $"/uploads/canteens/{fileName}";
        _db.SaveChanges();
        return Ok("Cập nhật ảnh căng tin thành công.");
    }

    [HttpPatch("{id}/status")]
    public ActionResult UpdateStatus(int id, UpdateCanteenStatusRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == id);
        if (canteen == null) return NotFound("Không tìm thấy căng tin.");

        canteen.Status = request.Status;
        _db.SaveChanges();
        return Ok("Cập nhật trạng thái căng tin thành công.");
    }
}
