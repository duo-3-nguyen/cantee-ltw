using Backend.Enums;

namespace Backend.DTOs;

public class CreateRegistrationRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CanteenName { get; set; } = string.Empty;
    public string CanteenAddress { get; set; } = string.Empty;
    public string CanteenPhoneNumber { get; set; } = string.Empty;
    public string CanteenEmail { get; set; } = string.Empty;
}

public class RegistrationRequestResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CanteenName { get; set; } = string.Empty;
    public string CanteenAddress { get; set; } = string.Empty;
    public string CanteenPhoneNumber { get; set; } = string.Empty;
    public string CanteenEmail { get; set; } = string.Empty;
    public RegistrationRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
