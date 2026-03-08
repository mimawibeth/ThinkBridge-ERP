namespace ThinkBridge_ERP.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string firstName, string temporaryPassword);
}
