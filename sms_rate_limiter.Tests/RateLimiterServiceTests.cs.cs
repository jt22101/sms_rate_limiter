using Microsoft.Extensions.Options;
using sms_rate_limiter.Models;
using sms_rate_limiter.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace sms_rate_limiter.Tests
{
    public class RateLimiterServiceTests
    {
        private readonly RateLimitConfig _config;
        private readonly RateLimiterService _rateLimiter;

        public RateLimiterServiceTests()
        {
            _config = new RateLimitConfig
            {
                MaxMessagesPerNumberPerSecond = 2,
                MaxMessagesPerAccountPerSecond = 5,
                InactivityThresholdMinutes = 30,
                CleanupIntervalMinutes = 5
            };

            var options = Options.Create(_config);
            _rateLimiter = new RateLimiterService(options);
        }

        [Fact]
        public async Task CanSendMessage_UnderLimit_ReturnsTrue()
        {
            // Arrange
            var phoneNumber = "+1234567890";

            // Act
            var result = await _rateLimiter.CanSendMessageAsync(phoneNumber);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanSendMessage_NumberLimitExceeded_ReturnsFalse()
        {
            // Arrange
            var phoneNumber = "+1234567890";

            // Act
            await _rateLimiter.RecordMessageSentAsync(phoneNumber);
            await _rateLimiter.RecordMessageSentAsync(phoneNumber);
            var result = await _rateLimiter.CanSendMessageAsync(phoneNumber);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanSendMessage_AccountLimitExceeded_ReturnsFalse()
        {
            // Arrange
            var phoneNumbers = new[]
            {
                "+1234567890",
                "+1234567891",
                "+1234567892",
                "+1234567893",
                "+1234567894"
            };

            // Act
            foreach (var number in phoneNumbers)
            {
                await _rateLimiter.RecordMessageSentAsync(number);
            }

            var result = await _rateLimiter.CanSendMessageAsync("+1234567895");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanSendMessage_EmptyPhoneNumber_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _rateLimiter.CanSendMessageAsync(string.Empty));
        }

        [Fact]
        public async Task CleanupInactiveNumbers_RemovesOldEntries()
        {
            // Arrange
            var phoneNumber = "+1234567890";
            await _rateLimiter.RecordMessageSentAsync(phoneNumber);

            // Simulate waiting by forcing cleanup
            await _rateLimiter.CleanupInactiveNumbersAsync();

            // Act - Try to send a new message
            var result = await _rateLimiter.CanSendMessageAsync(phoneNumber);

            // Assert - Should be true as the old record should be cleaned up
            Assert.True(result);
        }

        [Fact]
        public async Task RecordMessageSent_EmptyPhoneNumber_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _rateLimiter.RecordMessageSentAsync(string.Empty));
        }
    }
}