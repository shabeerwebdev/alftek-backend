using System.Text;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.BankPaymentFiles.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Generates bank payment transfer files in WPS (UAE), CIMB IBG (Malaysia), and NEFT (India) formats.
/// </summary>
public class BankPaymentFileService : IBankPaymentFileService
{
    private readonly HrmsDbContext _context;

    public BankPaymentFileService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> GenerateAsync(
        Guid payrollRunId, string format, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == payrollRunId, ct)
            ?? throw new InvalidOperationException("Payroll run not found");

        var payslips = await _context.Payslips
            .Include(p => p.Employee)
            .Where(p => p.PayrollRunId == payrollRunId && p.NetPay > 0)
            .OrderBy(p => p.Employee.EmployeeCode)
            .ToListAsync(ct);

        // Load primary bank accounts for these employees
        var employeeIds = payslips.Select(p => p.EmployeeId).ToList();
        var bankAccounts = await _context.EmployeeBankAccounts
            .Where(b => employeeIds.Contains(b.EmployeeId) && b.IsPrimary)
            .ToDictionaryAsync(b => b.EmployeeId, ct);

        var salaryMonth = $"{run.Year}{run.Month:D2}";
        var periodLabel = $"{new System.Globalization.CultureInfo("en-US").DateTimeFormat.GetMonthName(run.Month)}{run.Year}";

        return format.ToLowerInvariant() switch
        {
            "wps" => GenerateWps(payslips, bankAccounts, salaryMonth, periodLabel),
            "cimb" => GenerateCimb(payslips, bankAccounts, periodLabel),
            "neft" => GenerateNeft(payslips, bankAccounts, salaryMonth),
            _ => throw new InvalidOperationException($"Unsupported format '{format}'. Use: wps, cimb, neft")
        };
    }

    // ── WPS ─────────────────────────────────────────────────────────────────
    // UAE Wage Protection System SIF (Salary Information File) format.
    // EDR header record + EMP detail records.
    private static (byte[], string, string) GenerateWps(
        List<Domain.Entities.Payroll.Payslip> payslips,
        Dictionary<Guid, Domain.Entities.CoreHR.EmployeeBankAccount> accounts,
        string salaryMonth,
        string periodLabel)
    {
        var sb = new StringBuilder();
        decimal totalAmount = 0;
        var details = new StringBuilder();
        int seq = 1;

        foreach (var p in payslips)
        {
            if (!accounts.TryGetValue(p.EmployeeId, out var acct)) continue;

            var routingCode = acct.BranchCode ?? "000";
            var iban = acct.IbanNumber ?? acct.AccountNumber;
            var amount = p.NetPay.ToString("F2");
            totalAmount += p.NetPay;

            // EMP|Seq|RoutingCode|IBAN/Account|Amount|EmployeeCode|EmployeeName|SalaryMonth
            details.AppendLine(
                $"EMP|{seq++:D6}|{routingCode}|{iban}|{amount}|{p.Employee.EmployeeCode}|{Sanitize(p.Employee.FullName)}|{salaryMonth}");
        }

        var recordCount = seq - 1;
        // EDR header: EDR|EmployerRoutingCode|CompanyName|RecordCount|TotalAmount|SalaryMonth
        sb.AppendLine($"EDR|000000|AlfTekPro|{recordCount:D6}|{totalAmount:F2}|{salaryMonth}");
        sb.Append(details);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return (bytes, $"wps-salary-{periodLabel}.sif", "text/plain");
    }

    // ── CIMB IBG ─────────────────────────────────────────────────────────────
    // Malaysia Interbank GIRO CSV format.
    private static (byte[], string, string) GenerateCimb(
        List<Domain.Entities.Payroll.Payslip> payslips,
        Dictionary<Guid, Domain.Entities.CoreHR.EmployeeBankAccount> accounts,
        string periodLabel)
    {
        var sb = new StringBuilder();
        // Header row
        sb.AppendLine("\"Payment Type\",\"Beneficiary Name\",\"Bank Code\",\"Account Number\",\"Amount\",\"Reference\",\"Currency\"");

        foreach (var p in payslips)
        {
            if (!accounts.TryGetValue(p.EmployeeId, out var acct)) continue;

            var bankCode = acct.BranchCode ?? acct.SwiftCode ?? "CIMB";
            var reference = $"SAL-{p.Employee.EmployeeCode}-{periodLabel}";
            sb.AppendLine(
                $"\"IBG\",\"{CsvEscape(p.Employee.FullName)}\",\"{bankCode}\",\"{acct.AccountNumber}\",\"{p.NetPay:F2}\",\"{reference}\",\"MYR\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return (bytes, $"cimb-salary-{periodLabel}.csv", "text/csv");
    }

    // ── NEFT ─────────────────────────────────────────────────────────────────
    // India NEFT payment file — pipe-delimited text accepted by most Indian core-banking systems.
    private static (byte[], string, string) GenerateNeft(
        List<Domain.Entities.Payroll.Payslip> payslips,
        Dictionary<Guid, Domain.Entities.CoreHR.EmployeeBankAccount> accounts,
        string salaryMonth)
    {
        var sb = new StringBuilder();
        // Header
        sb.AppendLine("SEQ|TXNMODE|BENE_IFSC|BENE_ACCOUNT|BENE_NAME|AMOUNT|PAYMENT_REF|PURPOSE_CODE");

        int seq = 1;
        foreach (var p in payslips)
        {
            if (!accounts.TryGetValue(p.EmployeeId, out var acct)) continue;

            var ifsc = acct.BranchCode ?? "UNKNOWN";
            var reference = $"SAL{salaryMonth}{p.Employee.EmployeeCode}";
            sb.AppendLine(
                $"{seq++}|NEFT|{ifsc}|{acct.AccountNumber}|{Sanitize(p.Employee.FullName)}|{p.NetPay:F2}|{reference}|SAL");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return (bytes, $"neft-salary-{salaryMonth}.txt", "text/plain");
    }

    // Strip pipe/comma/newlines from name fields to avoid injection into delimited formats
    private static string Sanitize(string value)
        => value.Replace("|", " ").Replace("\n", " ").Replace("\r", "");

    private static string CsvEscape(string value)
        => value.Replace("\"", "\"\"");
}
