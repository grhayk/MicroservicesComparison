using NotificationService.Models;
using System.Diagnostics;

namespace NotificationService.Services
{
    public interface IEmailService
    {
        Task<NotificationResult> SendEmailAsync(NotificationMessage message);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task<NotificationResult> SendEmailAsync(NotificationMessage message)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Sending email to {Recipient} - Subject: {Subject}",
                    message.RecipientEmail, message.Subject);

                // Simulate email sending delay (real email service would take time)
                await Task.Delay(Random.Shared.Next(100, 500));

                // In a real application, you would use an email service like:
                // - SendGrid
                // - Amazon SES
                // - SMTP server
                // - etc.

                stopwatch.Stop();

                _logger.LogInformation(
                    "✉️  EMAIL SENT ✉️\n" +
                    "  To: {Recipient}\n" +
                    "  Subject: {Subject}\n" +
                    "  Type: {Type}\n" +
                    "  Processing Time: {Duration}ms\n" +
                    "  Created At: {CreatedAt}\n" +
                    "  Body: {Body}",
                    message.RecipientEmail,
                    message.Subject,
                    message.Type,
                    stopwatch.Elapsed.TotalMilliseconds,
                    message.CreatedAt,
                    message.Body
                );

                // Log metadata if available
                if (message.Metadata.Any())
                {
                    _logger.LogInformation("  Metadata:");
                    foreach (var (key, value) in message.Metadata)
                    {
                        _logger.LogInformation("    {Key}: {Value}", key, value);
                    }
                }

                return new NotificationResult
                {
                    Success = true,
                    Message = $"Email sent successfully to {message.RecipientEmail}",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to send email to {Recipient}", message.RecipientEmail);

                return new NotificationResult
                {
                    Success = false,
                    Message = $"Failed to send email: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
        }
    }
}
