using Backend.Enums;

namespace Backend.DTOs;

public class OperatingHourResponse
{
    public int Id { get; set; }
    public WeekDay DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

public class UpdateOperatingHourItem
{
    public WeekDay DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

public class UpdateOperatingHoursRequest
{
    public List<UpdateOperatingHourItem> Hours { get; set; } = new();
}
