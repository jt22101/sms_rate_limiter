using Microsoft.Extensions.Options;
using sms_rate_limiter.Models;
using sms_rate_limiter.Services.Interfaces;
using System.Collections.Concurrent;

namespace sms_rate_limiter.Services
{
    public class RateLimiterService : IRateLimiter
    {
        private readonly RateLimitConfig _config;
        private readonly ConcurrentDictionary<string, MessageTrackingData> _numberTracking;
        private readonly ConcurrentDictionary<long, int> _accountTracking;
        private readonly object _accountLock = new object();
        private long _currentSecond;
        private Timer _cleanupTimer;

        public RateLimiterService(IOptions<RateLimitConfig> config)
        {
            _config = config.Value;
            _numberTracking = new ConcurrentDictionary<string, MessageTrackingData>();
            _accountTracking = new ConcurrentDictionary<long, int>();
            _currentSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Initialize cleanup timer
            _cleanupTimer = new Timer(
                async _ => await CleanupInactiveNumbersAsync(),
                null,
                TimeSpan.FromMinutes(_config.CleanupIntervalMinutes),
                TimeSpan.FromMinutes(_config.CleanupIntervalMinutes)
            );
        }

        public async Task<bool> CanSendMessageAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

            var currentSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Reset account tracking if its a new second
            if (currentSecond != _currentSecond)
            {
                lock (_accountLock)
                {
                    if (currentSecond != _currentSecond)
                    {
                        _accountTracking.Clear();
                        _currentSecond = currentSecond;
                    }
                }
            }

            // Check account-wide limit
            var currentAccountCount = _accountTracking.GetOrAdd(currentSecond, 0);
            if (currentAccountCount >= _config.MaxMessagesPerAccountPerSecond)
                return false;

            // Check number-specific limit
            var numberData = _numberTracking.GetOrAdd(phoneNumber, _ => new MessageTrackingData
            {
                MessageCount = 0,
                LastMessageTime = DateTimeOffset.UtcNow
            });

            // Reset counter if we're in a new second
            if (numberData.LastMessageTime.ToUnixTimeSeconds() != currentSecond)
            {
                numberData.MessageCount = 0;
                numberData.LastMessageTime = DateTimeOffset.UtcNow;
            }

            return numberData.MessageCount < _config.MaxMessagesPerNumberPerSecond;
        }

        public async Task RecordMessageSentAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

            var currentSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Update number-specific tracking
            _numberTracking.AddOrUpdate(
                phoneNumber,
                new MessageTrackingData
                {
                    MessageCount = 1,
                    LastMessageTime = DateTimeOffset.UtcNow
                },
                (_, data) =>
                {
                    if (data.LastMessageTime.ToUnixTimeSeconds() != currentSecond)
                    {
                        data.MessageCount = 0;
                    }
                    data.MessageCount++;
                    data.LastMessageTime = DateTimeOffset.UtcNow;
                    return data;
                });

            // Update account-wide tracking
            _accountTracking.AddOrUpdate(currentSecond, 1, (_, count) => count + 1);
        }

        public async Task CleanupInactiveNumbersAsync()
        {
            var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-_config.InactivityThresholdMinutes);

            foreach (var number in _numberTracking.Keys)
            {
                if (_numberTracking.TryGetValue(number, out var data) &&
                    data.LastMessageTime < cutoffTime)
                {
                    _numberTracking.TryRemove(number, out _);
                }
            }
        }
    }
}