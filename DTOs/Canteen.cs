using Backend.Enums;

namespace Backend.DTOs;

public class CanteenResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public CanteenStatus Status { get; set; }
    public int StaffId { get; set; }
    public List<OperatingHourResponse> OperatingHours { get; set; } = new();
}

public class UpdateCanteenRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class UpdateCanteenStatusRequest
{
    public CanteenStatus Status { get; set; }
}
