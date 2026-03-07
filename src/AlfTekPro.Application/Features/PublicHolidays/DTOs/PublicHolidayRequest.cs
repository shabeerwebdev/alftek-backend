namespace AlfTekPro.Application.Features.PublicHolidays.DTOs;

public class PublicHolidayRequest
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = null!;
    public bool IsRecurring { get; set; }
    public string? Description { get; set; }
}
