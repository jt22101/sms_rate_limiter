using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using sms_rate_limiter.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace sms_rate_limiter.Tests
{
    public class RateLimitControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public RateLimitControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Configure test services
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure test rate limits
                    services.Configure<RateLimitConfig>(options =>
                    {
                        options.MaxMessagesPerNumberPerSecond = 2;
                        options.MaxMessagesPerAccountPerSecond = 5;
                        options.InactivityThresholdMinutes = 1;
                        options.CleanupIntervalMinutes = 1;
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CheckCanSendMessage_ValidNumber_ReturnsTrue()
        {
            // Arrange
            var phoneNumber = "+1234567890";

            // Act
            var response = await _client.GetAsync($"/api/ratelimit/check/{phoneNumber}");
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(content?["canSend"]);
        }

        [Fact]
        public async Task CheckCanSendMessage_ExceedsNumberLimit_ReturnsFalse()
        {
            // Arrange
            var phoneNumber = "+1234567891";

            // Act - Record two messages
            await _client.PostAsync($"/api/ratelimit/record/{phoneNumber}", null);
            await _client.PostAsync($"/api/ratelimit/record/{phoneNumber}", null);

            // Check if can send third message
            var response = await _client.GetAsync($"/api/ratelimit/check/{phoneNumber}");
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(content?["canSend"]);
        }

        [Fact]
        public async Task CheckCanSendMessage_ExceedsAccountLimit_ReturnsFalse()
        {
            // Arrange
            var phoneNumbers = new[]
            {
                "+1234567892",
                "+1234567893",
                "+1234567894",
                "+1234567895",
                "+1234567896"
            };

            // Act - Record messages from different numbers
            foreach (var number in phoneNumbers)
            {
                await _client.PostAsync($"/api/ratelimit/record/{number}", null);
            }

            // Check if can send another message
            var response = await _client.GetAsync("/api/ratelimit/check/+1234567897");
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(content?["canSend"]);
        }

        [Fact]
        public async Task RecordMessageSent_ValidNumber_ReturnsOk()
        {
            // Arrange
            var phoneNumber = "+1234567898";

            // Act
            var response = await _client.PostAsync($"/api/ratelimit/record/{phoneNumber}", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CleanupEndpoint_ReturnsOk()
        {
            // Act
            var response = await _client.PostAsync("/api/ratelimit/cleanup", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CheckCanSendMessage_InvalidNumber_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/ratelimit/check/%20");  // URL encoded space

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}