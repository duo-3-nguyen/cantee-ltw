using System.Security.Cryptography;
using System.Text;
using Backend.Data;
using Backend.Models;
using Backend.Enums;

namespace Backend.Services;

public class AuthChecker
{
    private readonly MyDbContext _db;

    public AuthChecker(MyDbContext db)
    {
        _db = db;
    }

    public (bool Success, string? Error, User? user, Session? session) RequireLogin(HttpRequest request)
    {
        string? sessionId = request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionId)) return (false, "Chưa đăng nhập.", null, null);

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));
        var session = _db.Sessions.FirstOrDefault(s =>
            s.SessionIdHash == hash &&
            s.ExpiresAt > DateTime.UtcNow);
        if (session == null) return (false, "Phiên làm việc không hợp lệ hoặc đã hết hạn.", null, null);

        var user = _db.Users.FirstOrDefault(u => u.Id == session.UserId);
        if (user == null) return (false, "Người dùng không tồn tại.", null, session);

        if (!user.IsActive) return (false, "Tài khoản đã bị vô hiệu hóa.", null, session);

        return (true, null, user, session);
    }

    public bool RequireRoles(User user, params UserRole[] roles)
    {
        if (!roles.Contains(user.Role)) return false;

        return true;
    }
}