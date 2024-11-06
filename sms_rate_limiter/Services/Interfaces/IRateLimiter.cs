using System.Threading.Tasks;

namespace sms_rate_limiter.Services.Interfaces
{
    public interface IRateLimiter
    {
        /// <summary>
        /// Checks if a message can be sent from the specified phone number without exceeding rate limits
        /// </summary>
        /// <param name="phoneNumber">The business phone number attempting to send the message</param>
        /// <returns>True if the message can be sent, false if it would exceed limits</returns>
        Task<bool> CanSendMessageAsync(string phoneNumber);

        /// <summary>
        /// Records that a message was sent from the specified phone number
        /// </summary>
        /// <param name="phoneNumber">The business phone number that sent the message</param>
        /// <returns>Task representing the async operation</returns>
        Task RecordMessageSentAsync(string phoneNumber);

        /// <summary>
        /// Cleans up tracking data for inactive phone numbers
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task CleanupInactiveNumbersAsync();
    }
}