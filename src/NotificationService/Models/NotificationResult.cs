namespace NotificationService.Models
{
    public class NotificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public double ProcessingTimeMs { get; set; }
    }
}
