namespace AlfTekPro.Application.Features.Reports.Interfaces;

public interface IReportService
{
    Task<ReportResult> EmployeeDirectoryAsync(Guid tenantId, CancellationToken ct = default);

    Task<ReportResult> AttendanceSummaryAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default);

    Task<ReportResult> LeaveBalanceAsync(
        Guid tenantId, int year, CancellationToken ct = default);

    Task<ReportResult> PayrollSummaryAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default);
}

public class ReportResult
{
    public string ReportName { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int TotalRows => Rows.Count;

    public string ToCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", Headers.Select(EscapeCsv)));
        foreach (var row in Rows)
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
