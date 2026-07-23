using Backend.Enums;

namespace Backend.Models;

public class Canteen
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public CanteenStatus Status { get; set; } = CanteenStatus.Active;

    public int StaffId { get; set; }
    public User Staff { get; set; } = null!;

    public List<Product> Products { get; set; } = new();
    public List<OperatingHour> OperatingHours { get; set; } = new();
}
