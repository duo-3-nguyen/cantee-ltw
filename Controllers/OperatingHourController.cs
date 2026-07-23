using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/canteens/{canteenId}/operating-hours")]
public class OperatingHourController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public OperatingHourController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpGet]
    public ActionResult<List<OperatingHourResponse>> GetAll(int canteenId)
    {
        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căng tin.");

        var hours = _db.OperatingHours
            .Where(o => o.CanteenId == canteenId)
            .OrderBy(o => o.DayOfWeek)
            .Select(o => new OperatingHourResponse
            {
                Id = o.Id,
                DayOfWeek = o.DayOfWeek,
                OpenTime = o.OpenTime,
                CloseTime = o.CloseTime,
                IsClosed = o.IsClosed
            }).ToList();

        return hours;
    }

    [HttpPut]
    public ActionResult Update(int canteenId, UpdateOperatingHoursRequest request)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Staff, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var canteen = _db.Canteens.FirstOrDefault(c => c.Id == canteenId);
        if (canteen == null) return NotFound("Không tìm thấy căng tin.");

        var existingHours = _db.OperatingHours.Where(o => o.CanteenId == canteenId).ToList();
        _db.OperatingHours.RemoveRange(existingHours);

        foreach (var item in request.Hours)
        {
            _db.OperatingHours.Add(new OperatingHour
            {
                CanteenId = canteenId,
                DayOfWeek = item.DayOfWeek,
                OpenTime = item.OpenTime,
                CloseTime = item.CloseTime,
                IsClosed = item.IsClosed
            });
        }

        _db.SaveChanges();
        return Ok("Cập nhật giờ hoạt động thành công.");
    }
}
