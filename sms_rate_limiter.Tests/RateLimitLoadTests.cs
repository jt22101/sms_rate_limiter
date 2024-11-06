using NBomber.CSharp;
using NBomber.Http.CSharp;
using NBomber.Contracts;
using NBomber.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using sms_rate_limiter.Models;
using Microsoft.Extensions.Options;
using NBomber.Contracts.Stats;

namespace sms_rate_limiter.Tests
{
    public class RateLimitLoadTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RateLimitLoadTests(WebApplicationFactory<Program> factory)
        {
            // Configure the test server with higher rate limits for high volume testing
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<RateLimitConfig>(options =>
                    {
                        options.MaxMessagesPerNumberPerSecond = 50;    // Increased limit per number
                        options.MaxMessagesPerAccountPerSecond = 500;  // Increased account-wide limit
                        options.InactivityThresholdMinutes = 30;
                        options.CleanupIntervalMinutes = 5;
                    });
                });
            });
        }

        [Fact]
        public async Task LoadTest_HighVolume_RateLimitEndpoints()
        {
            var client = _factory.CreateClient();

            // Verify setup with a test request
            var testResponse = await client.GetAsync("api/ratelimit/check/+11234567890");
            Console.WriteLine($"Initial test request status: {testResponse.StatusCode}");
            var testContent = await testResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Initial test content: {testContent}");

            // Higher volume check rate scenario
            var checkRateScenario = Scenario.Create("check_rate_limit", async context =>
            {
                var phoneNumber = $"+1{Random.Shared.Next(100000000, 999999999)}";
                var response = await client.GetAsync($"api/ratelimit/check/{phoneNumber}");
                var content = await response.Content.ReadAsStringAsync();

                return Response.Ok(response.StatusCode.ToString(), response.StatusCode.ToString());
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 200,     // 200 requests per second
                                interval: TimeSpan.FromSeconds(1),
                                during: TimeSpan.FromSeconds(30))  // Extended duration to 30 seconds
            );

            // Higher volume record scenario
            var recordScenario = Scenario.Create("record_message", async context =>
            {
                var phoneNumber = $"+1{Random.Shared.Next(100000000, 999999999)}";
                var response = await client.PostAsync($"api/ratelimit/record/{phoneNumber}", null);

                return Response.Ok(response.StatusCode.ToString(), response.StatusCode.ToString());
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 200,     // 200 requests per second
                                interval: TimeSpan.FromSeconds(1),
                                during: TimeSpan.FromSeconds(30))  // Extended duration to 30 seconds
            );

            Console.WriteLine("\nStarting high volume load test...");

            var result = NBomberRunner
                .RegisterScenarios(checkRateScenario, recordScenario)
                .WithTestName("High Volume Rate Limit API Test")
                .WithReportFileName($"high_volume_test_{DateTime.Now:yyyyMMdd_HHmmss}")
                .WithReportFormats(ReportFormat.Txt, ReportFormat.Html)
                .Run();

            Console.WriteLine($"\nTest completed. AllOkCount: {result.AllOkCount}");

            foreach (var stats in result.ScenarioStats)
            {
                Console.WriteLine($"\nScenario: {stats.ScenarioName}");
                Console.WriteLine($"Duration: {stats.Duration.TotalSeconds:F1} seconds");
                Console.WriteLine($"OK requests: {stats.Ok.Request.Count}");
                Console.WriteLine($"Failed requests: {stats.Fail.Request.Count}");
                Console.WriteLine($"RPS: {stats.Ok.Request.Count / stats.Duration.TotalSeconds:F1}");

                if (stats.Ok.Latency != null)
                {
                    Console.WriteLine($"Mean latency: {stats.Ok.Latency.MeanMs:F1}ms");
                    Console.WriteLine($"Max latency: {stats.Ok.Latency.MaxMs:F1}ms");
                    Console.WriteLine($"99th percentile latency: {stats.Ok.Latency.Percent99:F1}ms");
                }

                // Performance assertions
                Assert.True(stats.Ok.Request.Count > 0,
                    $"Should have successful requests in {stats.ScenarioName}");

                // Verify handling high volume
                var actualRps = stats.Ok.Request.Count / stats.Duration.TotalSeconds;
                Assert.True(actualRps >= 100,
                    $"Should handle at least 100 RPS, actual: {actualRps:F1}");

                // Verify response times stay reasonable under load
                if (stats.Ok.Latency != null)
                {
                    Assert.True(stats.Ok.Latency.Percent99 < 500,
                        $"99th percentile latency should be under 500ms, actual: {stats.Ok.Latency.Percent99:F1}ms");
                }
            }
        }
    }
}