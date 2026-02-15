namespace AlfTekPro.Application.Features.Regions.DTOs;

/// <summary>
/// Response DTO for region information
/// </summary>
public class RegionResponse
{
    /// <summary>
    /// Region unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Region code (UAE, USA, IND)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Region full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Currency code (AED, USD, INR)
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Date format pattern (dd/MM/yyyy, MM/dd/yyyy)
    /// </summary>
    public string DateFormat { get; set; } = string.Empty;

    /// <summary>
    /// Text direction (ltr, rtl)
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Language code (en, ar, hi)
    /// </summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Timezone identifier (Asia/Dubai, America/New_York, Asia/Kolkata)
    /// </summary>
    public string Timezone { get; set; } = string.Empty;
}
