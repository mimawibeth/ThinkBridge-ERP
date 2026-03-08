using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailService> _logger;

    private const string RESEND_API_URL = "https://api.resend.com/emails";

    public EmailService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<EmailService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Sends a password reset email with the temporary password via Resend API.
    /// </summary>
    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string temporaryPassword)
    {
        try
        {
            var apiKey = _config["Resend:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Resend API key is not configured. Skipping email send.");
                return false;
            }

            var fromEmail = _config["Resend:FromEmail"] ?? "ThinkBridge ERP <onboarding@resend.dev>";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var emailBody = new
            {
                from = fromEmail,
                to = new[] { toEmail },
                subject = "ThinkBridge ERP - Your Password Has Been Reset",
                html = BuildPasswordResetHtml(firstName, temporaryPassword)
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(emailBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(RESEND_API_URL, jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Resend API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return false;
            }

            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
            return false;
        }
    }

    private static string BuildPasswordResetHtml(string firstName, string temporaryPassword)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0; padding:0; background-color:#f4f6f8; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f6f8; padding:40px 20px;"">
        <tr>
            <td align=""center"">
                <table width=""500"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 2px 12px rgba(0,0,0,0.08);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0B3D4C 0%, #1A6B8A 100%); padding:32px 40px; text-align:center;"">
                            <h1 style=""margin:0; color:#ffffff; font-size:22px; font-weight:700;"">
                                <span style=""color:#ffffff;"">Think</span><span style=""color:#F97316;"">Bridge</span> ERP
                            </h1>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding:36px 40px;"">
                            <h2 style=""margin:0 0 16px; color:#1a1a2e; font-size:18px; font-weight:600;"">Password Reset</h2>
                            <p style=""margin:0 0 20px; color:#64748b; font-size:14px; line-height:1.6;"">
                                Hi {System.Net.WebUtility.HtmlEncode(firstName)},
                            </p>
                            <p style=""margin:0 0 20px; color:#64748b; font-size:14px; line-height:1.6;"">
                                Your password has been reset by your company administrator. Please use the temporary password below to log in, and you will be prompted to create a new password.
                            </p>
                            <!-- Password Box -->
                            <div style=""background-color:#f0f9ff; border:1px solid #bae6fd; border-radius:8px; padding:20px; text-align:center; margin:24px 0;"">
                                <p style=""margin:0 0 8px; color:#64748b; font-size:12px; text-transform:uppercase; letter-spacing:1px;"">Temporary Password</p>
                                <p style=""margin:0; color:#0B3D4C; font-size:20px; font-weight:700; font-family:monospace; letter-spacing:1px;"">{System.Net.WebUtility.HtmlEncode(temporaryPassword)}</p>
                            </div>
                            <p style=""margin:24px 0 0; color:#94a3b8; font-size:13px; line-height:1.5;"">
                                For your security, please change your password immediately after logging in. Do not share this email with anyone.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""padding:20px 40px; background-color:#f8fafc; border-top:1px solid #e2e8f0; text-align:center;"">
                            <p style=""margin:0; color:#94a3b8; font-size:12px;"">
                                &copy; 2026 ThinkBridge ERP. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
