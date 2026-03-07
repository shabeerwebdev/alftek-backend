namespace AlfTekPro.Application.Features.EmergencyContacts.DTOs;

public class EmergencyContactRequest
{
    public string Name { get; set; } = null!;
    public string Relationship { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsPrimary { get; set; }
}
