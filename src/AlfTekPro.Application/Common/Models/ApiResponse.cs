namespace AlfTekPro.Application.Common.Models;

/// <summary>
/// Standard API response wrapper for consistent response format
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data payload
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error code for internationalization (i18n)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Validation errors keyed by field name
    /// </summary>
    public Dictionary<string, string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponse<T> SuccessResult(T? data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error response with message
    /// </summary>
    public static ApiResponse<T> ErrorResult(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Creates a validation error response with field-specific errors
    /// </summary>
    public static ApiResponse<T> ValidationErrorResult(Dictionary<string, string> errors, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message ?? "Validation failed",
            ErrorCode = "VALIDATION_ERROR",
            Errors = errors
        };
    }
}
