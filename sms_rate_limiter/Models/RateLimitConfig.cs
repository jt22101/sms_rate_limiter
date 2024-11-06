namespace sms_rate_limiter.Models
{
    public class RateLimitConfig
    {
        /// <summary>
        /// Maximum number of messages allowed per phone number per second
        /// </summary>
        public int MaxMessagesPerNumberPerSecond { get; set; }

        /// <summary>
        /// Maximum number of messages allowed across the entire account per second
        /// </summary>
        public int MaxMessagesPerAccountPerSecond { get; set; }

        /// <summary>
        /// Time in minutes after which a phone number is considered inactive
        /// </summary>
        public int InactivityThresholdMinutes { get; set; } = 30;

        /// <summary>
        /// Interval in minutes for running the cleanup of inactive numbers
        /// </summary>
        public int CleanupIntervalMinutes { get; set; } = 5;
    }

    public class MessageTrackingData
    {
        /// <summary>
        /// Number of messages sent in the current second
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Timestamp of the last message sent
        /// </summary>
        public DateTimeOffset LastMessageTime { get; set; }
    }
}