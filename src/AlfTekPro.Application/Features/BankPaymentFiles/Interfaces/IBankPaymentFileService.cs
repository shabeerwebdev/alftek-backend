namespace AlfTekPro.Application.Features.BankPaymentFiles.Interfaces;

/// <summary>
/// Generates bank payment transfer files for a completed payroll run.
/// Supported formats: wps (UAE Wage Protection System), cimb (Malaysia IBG), neft (India NEFT).
/// </summary>
public interface IBankPaymentFileService
{
    Task<(byte[] Content, string FileName, string ContentType)> GenerateAsync(
        Guid payrollRunId, string format, CancellationToken ct = default);
}
