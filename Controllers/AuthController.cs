using Backend.Models;
using Backend.Enums;
using Backend.Data;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Helpers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly int _sessionTimeoutDays = 7;
    private readonly AuthChecker _authChecker;

    public AuthController(MyDbContext context, AuthChecker authChecker)
    {
        _db = context;
        _authChecker = authChecker;
    }

    [HttpPost("register")]
    public ActionResult<UserResponse> Register(RegisterRequest request)
    {
        var authCheckResult = _authChecker.RequireLogin(Request);
        if (authCheckResult.Success) return Conflict("Bạn đã đăng nhập. Vui lòng đăng xuất trước khi đăng ký tài khoản mới.");

        var existingUser = _db.Users.FirstOrDefault(u => u.Username == request.Username);
        if (existingUser != null) return Conflict("Username đã tồn tại");

        existingUser = _db.Users.FirstOrDefault(u => u.Email == request.Email);
        if (existingUser != null) return Conflict("Email đã được sử dụng");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Customer,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        _db.SaveChanges();

        Console.WriteLine($"User {user.Username} registered successfully");

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
    }

    [HttpPost("login")]
    public ActionResult Login(LoginRequest request)
    {
        var (success, _, _, _) = _authChecker.RequireLogin(Request);
        if (success) return Conflict("Bạn đã đăng nhập. Vui lòng đăng xuất trước khi đăng ký.");

        var user = _db.Users.FirstOrDefault(u => u.Username == request.Username);
        if (user == null) return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");

        if (!user.IsActive) return Unauthorized("Tài khoản đã bị vô hiệu hóa.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng");

        string sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string sessionIdHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));

        var session = new Session
        {
            SessionIdHash = sessionIdHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_sessionTimeoutDays)
        };
        _db.Sessions.Add(session);
        _db.SaveChanges();

        Response.Cookies.Append("SessionId", sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = session.ExpiresAt
        });

        Console.WriteLine($"User {user.Username} đã đăng nhập thành công");

        return Ok("Đăng nhập thành công");
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        var (success, error, user, session) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);

        _db.Sessions.Remove(session);
        _db.SaveChanges();

        Response.Cookies.Delete("SessionId");

        return Ok("Đăng xuất thành công.");
    }

    [HttpPost("change-password")]
    public ActionResult ChangePassword(ChangePasswordRequest request)
    {
        var (success, error, user, session) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return BadRequest("Mật khẩu cũ không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        if (request.logoutAllDevices)
        {
            var sessions = _db.Sessions
                .Where(s => s.UserId == user.Id)
                .ToList();
            _db.Sessions.RemoveRange(sessions);
            _db.SaveChanges();
        }
        else
        {
            _db.Sessions.Remove(session);
            _db.SaveChanges();
        }

        Response.Cookies.Delete("SessionId");

        Console.WriteLine($"User {user.Username} changed password");

        return Ok("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
    }

    [HttpPut("profile")]
    public ActionResult<UserResponse> UpdateProfile(UpdateProfileRequest request)
    {
        var (success, error, user, session) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);

        var changed = false;

        if (request.FullName != null && request.FullName != user.FullName)
        {
            user.FullName = request.FullName;
            changed = true;
        }

        if (request.Email != null && request.Email != user.Email)
        {
            var existing = _db.Users.FirstOrDefault(u => u.Email == request.Email && u.Id != user.Id);
            if (existing != null) return Conflict("Email đã được sử dụng.");
            user.Email = request.Email;
            changed = true;
        }

        if (!changed) return BadRequest("Không có thông tin nào thay đổi.");

        _db.SaveChanges();

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
    }

    [HttpGet("me")]
    public ActionResult<UserResponse> GetCurrentUser()
    {
        var (success, error, user, session) = _authChecker.RequireLogin(Request);
        if (!success) return Unauthorized(error);

        session.ExpiresAt = DateTime.UtcNow.AddDays(_sessionTimeoutDays);
        _db.SaveChanges();

        string sessionId = Request.Cookies["SessionId"];
        Response.Cookies.Append("SessionId", sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = session.ExpiresAt
        });

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}