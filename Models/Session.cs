namespace Backend.Models;

public class Session
{
    public int Id { get; set; }
    public string SessionIdHash { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    
    
    public User User { get; set; } = null!;
}