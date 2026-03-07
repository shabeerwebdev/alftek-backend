namespace AlfTekPro.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendLeaveApprovedAsync(string toEmail, string employeeName, string leaveType, DateTime from, DateTime to, CancellationToken ct = default);
    Task SendLeaveRejectedAsync(string toEmail, string employeeName, string leaveType, string reason, CancellationToken ct = default);
    Task SendPayslipPublishedAsync(string toEmail, string employeeName, string month, string year, CancellationToken ct = default);
    Task SendWelcomeNewTenantAsync(string toEmail, string adminName, string companyName, string loginUrl, CancellationToken ct = default);
    Task SendAccountLockedAsync(string toEmail, string employeeName, int lockoutMinutes, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string employeeName, string resetLink, CancellationToken ct = default);

    /// <summary>Generic send for custom scenarios.</summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
