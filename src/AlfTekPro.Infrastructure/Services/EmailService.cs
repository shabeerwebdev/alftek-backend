using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    private string SmtpHost => _config["Email:SmtpHost"] ?? string.Empty;
    private int    SmtpPort => int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
    private string SmtpUser => _config["Email:SmtpUser"] ?? string.Empty;
    private string SmtpPass => _config["Email:SmtpPassword"] ?? string.Empty;
    private string FromAddress => _config["Email:From"] ?? "noreply@alftekpro.com";
    private string FromName => _config["Email:FromName"] ?? "AlfTekPro HRMS";

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task SendLeaveApprovedAsync(string toEmail, string employeeName, string leaveType, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var body = Templates.LeaveApproved(employeeName, leaveType,
            from.ToString("dd MMM yyyy"), to.ToString("dd MMM yyyy"));
        return SendAsync(toEmail, "Your Leave Request Has Been Approved", body, ct);
    }

    public Task SendLeaveRejectedAsync(string toEmail, string employeeName, string leaveType, string reason, CancellationToken ct = default)
    {
        var body = Templates.LeaveRejected(employeeName, leaveType, reason);
        return SendAsync(toEmail, "Your Leave Request Has Been Declined", body, ct);
    }

    public Task SendPayslipPublishedAsync(string toEmail, string employeeName, string month, string year, CancellationToken ct = default)
    {
        var body = Templates.PayslipPublished(employeeName, month, year);
        return SendAsync(toEmail, $"Your Payslip for {month} {year} Is Ready", body, ct);
    }

    public Task SendWelcomeNewTenantAsync(string toEmail, string adminName, string companyName, string loginUrl, CancellationToken ct = default)
    {
        var body = Templates.WelcomeNewTenant(adminName, companyName, loginUrl);
        return SendAsync(toEmail, $"Welcome to AlfTekPro HRMS – {companyName}", body, ct);
    }

    public Task SendAccountLockedAsync(string toEmail, string employeeName, int lockoutMinutes, CancellationToken ct = default)
    {
        var body = Templates.AccountLocked(employeeName, lockoutMinutes);
        return SendAsync(toEmail, "Your Account Has Been Temporarily Locked", body, ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string employeeName, string resetLink, CancellationToken ct = default)
    {
        var body = Templates.PasswordReset(employeeName, resetLink);
        return SendAsync(toEmail, "Reset Your AlfTekPro Password", body, ct);
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(SmtpHost))
        {
            _logger.LogWarning("Email:SmtpHost is not configured. Skipping email to {To}: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls, ct);
            if (!string.IsNullOrWhiteSpace(SmtpUser))
                await client.AuthenticateAsync(SmtpUser, SmtpPass, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", toEmail, subject);
            throw;
        }
    }

    /// <summary>Inline HTML email templates.</summary>
    private static class Templates
    {
        private static string Wrap(string content) => $"""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"/></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;">
            <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;margin:0 auto;">
              <tr><td style="background:#1890ff;padding:20px 30px;">
                <h2 style="color:#fff;margin:0;">AlfTekPro HRMS</h2>
              </td></tr>
              <tr><td style="padding:30px;">
                {content}
              </td></tr>
              <tr><td style="background:#f0f0f0;padding:15px 30px;text-align:center;font-size:12px;color:#888;">
                This is an automated message from AlfTekPro HRMS. Please do not reply.
              </td></tr>
            </table>
            </body></html>
            """;

        public static string LeaveApproved(string name, string leaveType, string from, string to) => Wrap($"""
            <h3>Leave Approved ✅</h3>
            <p>Dear {name},</p>
            <p>Your <strong>{leaveType}</strong> leave request has been <strong>approved</strong>.</p>
            <p><strong>From:</strong> {from}<br/><strong>To:</strong> {to}</p>
            <p>Enjoy your time off!</p>
            """);

        public static string LeaveRejected(string name, string leaveType, string reason) => Wrap($"""
            <h3>Leave Request Declined ❌</h3>
            <p>Dear {name},</p>
            <p>Your <strong>{leaveType}</strong> leave request has been <strong>declined</strong>.</p>
            <p><strong>Reason:</strong> {reason}</p>
            <p>Please contact your manager for further clarification.</p>
            """);

        public static string PayslipPublished(string name, string month, string year) => Wrap($"""
            <h3>Payslip Ready 💰</h3>
            <p>Dear {name},</p>
            <p>Your payslip for <strong>{month} {year}</strong> is now available.</p>
            <p>Log in to the portal to view and download your payslip.</p>
            """);

        public static string WelcomeNewTenant(string adminName, string company, string loginUrl) => Wrap($"""
            <h3>Welcome to AlfTekPro HRMS 🎉</h3>
            <p>Dear {adminName},</p>
            <p>Your organisation <strong>{company}</strong> has been successfully set up on AlfTekPro HRMS.</p>
            <p>You can log in at:<br/><a href="{loginUrl}">{loginUrl}</a></p>
            <p>We recommend completing the setup wizard to configure your departments, locations, and payroll settings.</p>
            """);

        public static string AccountLocked(string name, int minutes) => Wrap($"""
            <h3>Account Temporarily Locked 🔒</h3>
            <p>Dear {name},</p>
            <p>Your account has been temporarily locked due to multiple failed login attempts.</p>
            <p>Please try again in <strong>{minutes} minute(s)</strong>.</p>
            <p>If you did not attempt to log in, please contact your administrator immediately.</p>
            """);

        public static string PasswordReset(string name, string resetLink) => Wrap($"""
            <h3>Password Reset Request 🔑</h3>
            <p>Dear {name},</p>
            <p>We received a request to reset your AlfTekPro password.</p>
            <p><a href="{resetLink}" style="background:#1890ff;color:#fff;padding:10px 20px;border-radius:4px;text-decoration:none;">Reset Password</a></p>
            <p>This link expires in 1 hour. If you did not request a password reset, please ignore this email.</p>
            """);
    }
}
