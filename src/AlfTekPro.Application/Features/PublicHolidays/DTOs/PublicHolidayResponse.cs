namespace AlfTekPro.Application.Features.PublicHolidays.DTOs;

public class PublicHolidayResponse
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = null!;
    public bool IsRecurring { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
