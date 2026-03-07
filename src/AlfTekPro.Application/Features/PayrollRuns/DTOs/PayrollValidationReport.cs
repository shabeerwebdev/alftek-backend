namespace AlfTekPro.Application.Features.PayrollRuns.DTOs;

public class PayrollValidationReport
{
    public int Month { get; set; }
    public int Year { get; set; }
    public bool CanProceed { get; set; }
    public int TotalActiveEmployees { get; set; }
    public int ReadyCount { get; set; }
    public List<PayrollValidationIssue> Issues { get; set; } = new();
}

public class PayrollValidationIssue
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning"; // "Error" blocks payroll; "Warning" is advisory
    public string Code { get; set; } = string.Empty;   // Machine-readable code e.g. MISSING_BANK_ACCOUNT
    public string Message { get; set; } = string.Empty;
}
