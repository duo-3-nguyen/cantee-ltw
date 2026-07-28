using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Enums;
using Backend.Models;
using Backend.Helpers;

namespace Backend.Controllers;

[ApiController]
[Route("api/registration-requests")]
public class CanteenRegistrationController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly AuthChecker _authChecker;

    public CanteenRegistrationController(MyDbContext db, AuthChecker authChecker)
    {
        _db = db;
        _authChecker = authChecker;
    }

    [HttpPost]
    public ActionResult Submit(CreateRegistrationRequest request)
    {
        var existingUser = _db.Users.FirstOrDefault(u => u.Username == request.Username);
        if (existingUser != null) return Conflict("Username đã tồn tại");

        existingUser = _db.Users.FirstOrDefault(u => u.Email == request.Email);
        if (existingUser != null) return Conflict("Email đã được sử dụng");

        var existingReq = _db.RegistrationRequests
            .FirstOrDefault(r => r.Username == request.Username && r.Status == RegistrationRequestStatus.Pending);
        if (existingReq != null) return Conflict("Bạn đã có yêu cầu đăng ký đang chờ duyệt.");

        var req = new RegistrationRequest
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            CanteenName = request.CanteenName,
            CanteenAddress = request.CanteenAddress,
            CanteenPhoneNumber = request.CanteenPhoneNumber,
            CanteenEmail = request.CanteenEmail,
            Status = RegistrationRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.RegistrationRequests.Add(req);
        _db.SaveChanges();

        return Ok("Đã gửi yêu cầu đăng ký. Vui lòng chờ admin duyệt.");
    }

    [HttpGet]
    public ActionResult<List<RegistrationRequestResponse>> GetAll([FromQuery] RegistrationRequestStatus? status)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var query = _db.RegistrationRequests.AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var list = query.OrderByDescending(r => r.CreatedAt).Select(r => new RegistrationRequestResponse
        {
            Id = r.Id,
            Username = r.Username,
            Email = r.Email,
            FullName = r.FullName,
            CanteenName = r.CanteenName,
            CanteenAddress = r.CanteenAddress,
            CanteenPhoneNumber = r.CanteenPhoneNumber,
            CanteenEmail = r.CanteenEmail,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        return list;
    }

    [HttpGet("{id}")]
    public ActionResult<RegistrationRequestResponse> GetById(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var req = _db.RegistrationRequests.FirstOrDefault(r => r.Id == id);
        if (req == null) return NotFound("Không tìm thấy yêu cầu đăng ký.");

        return new RegistrationRequestResponse
        {
            Id = req.Id,
            Username = req.Username,
            Email = req.Email,
            FullName = req.FullName,
            CanteenName = req.CanteenName,
            CanteenAddress = req.CanteenAddress,
            CanteenPhoneNumber = req.CanteenPhoneNumber,
            CanteenEmail = req.CanteenEmail,
            Status = req.Status,
            CreatedAt = req.CreatedAt
        };
    }

    [HttpPost("{id}/approve")]
    public ActionResult Approve(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var req = _db.RegistrationRequests.FirstOrDefault(r => r.Id == id);
        if (req == null) return NotFound("Không tìm thấy yêu cầu đăng ký.");
        if (req.Status != RegistrationRequestStatus.Pending)
            return BadRequest("Yêu cầu đã được xử lý trước đó.");

        var existingUser = _db.Users.FirstOrDefault(u => u.Username == req.Username);
        if (existingUser != null) return Conflict("Username đã tồn tại trong hệ thống.");

        var newUser = new User
        {
            Username = req.Username,
            Email = req.Email,
            PasswordHash = req.PasswordHash,
            FullName = req.FullName,
            Role = UserRole.Staff,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(newUser);
        _db.SaveChanges();

        var canteen = new Canteen
        {
            Name = req.CanteenName,
            Address = req.CanteenAddress,
            PhoneNumber = req.CanteenPhoneNumber,
            Email = req.CanteenEmail,
            StaffId = newUser.Id,
            Status = CanteenStatus.Active
        };
        _db.Canteens.Add(canteen);
        _db.SaveChanges();

        var defaultOperatingHours = new List<OperatingHour>();
        foreach (WeekDay day in Enum.GetValues<WeekDay>())
        {
            defaultOperatingHours.Add(new OperatingHour
            {
                CanteenId = canteen.Id,
                DayOfWeek = day,
                OpenTime = new TimeOnly(7, 0),
                CloseTime = new TimeOnly(17, 0),
                IsClosed = day == WeekDay.Sunday
            });
        }
        _db.OperatingHours.AddRange(defaultOperatingHours);

        req.Status = RegistrationRequestStatus.Approved;
        _db.SaveChanges();

        return Ok("Duyệt yêu cầu đăng ký thành công.");
    }

    [HttpPost("{id}/reject")]
    public ActionResult Reject(int id)
    {
        var (success, error, user, _) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);
        if (!_authChecker.RequireRoles(user, UserRole.Admin))
            return StatusCode(403, "Bạn không có quyền thực hiện hành động này.");

        var req = _db.RegistrationRequests.FirstOrDefault(r => r.Id == id);
        if (req == null) return NotFound("Không tìm thấy yêu cầu đăng ký.");
        if (req.Status != RegistrationRequestStatus.Pending)
            return BadRequest("Yêu cầu đã được xử lý trước đó.");

        req.Status = RegistrationRequestStatus.Rejected;
        _db.SaveChanges();

        return Ok("Từ chối yêu cầu đăng ký thành công.");
    }
}
