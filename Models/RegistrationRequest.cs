using System.ComponentModel.DataAnnotations;
using Backend.Enums;

namespace Backend.Models;

public class RegistrationRequest
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string CanteenName { get; set; } = string.Empty;
    public string CanteenAddress { get; set; } = string.Empty;
    public string CanteenPhoneNumber { get; set; } = string.Empty;
    public string CanteenEmail { get; set; } = string.Empty;
    
    public RegistrationRequestStatus Status { get; set; } = RegistrationRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
