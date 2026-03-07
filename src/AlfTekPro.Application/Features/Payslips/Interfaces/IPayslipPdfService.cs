namespace AlfTekPro.Application.Features.Payslips.Interfaces;

public interface IPayslipPdfService
{
    /// <summary>
    /// Generate a PDF byte array for a single payslip.
    /// </summary>
    Task<byte[]> GenerateAsync(Guid payslipId, CancellationToken ct = default);

    /// <summary>
    /// Generate a bundled PDF of all payslips in a payroll run and return the bytes.
    /// Caller is responsible for uploading to storage and persisting the path.
    /// </summary>
    Task<byte[]> GenerateBundleAsync(Guid payrollRunId, CancellationToken ct = default);
}
