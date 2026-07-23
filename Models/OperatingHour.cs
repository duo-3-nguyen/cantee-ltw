using Backend.Enums;

namespace Backend.Models;

public class OperatingHour
{
    public int Id { get; set; }
    public int CanteenId { get; set; }
    public WeekDay DayOfWeek { get; set; } = WeekDay.Sunday;
    public TimeOnly? OpenTime { get; set; } =  new TimeOnly(7, 0);
    public TimeOnly? CloseTime { get; set; } = new TimeOnly(17, 0);
    public bool IsClosed { get; set; } = false;

    public Canteen Canteen { get; set; } = null!;
}
