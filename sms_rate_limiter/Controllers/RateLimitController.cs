using Microsoft.AspNetCore.Mvc;
using sms_rate_limiter.Services.Interfaces;

namespace sms_rate_limiter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitController : ControllerBase
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly ILogger<RateLimitController> _logger;

        public RateLimitController(IRateLimiter rateLimiter, ILogger<RateLimitController> logger)
        {
            _rateLimiter = rateLimiter;
            _logger = logger;
        }

        [HttpGet("check/{phoneNumber}")]
        public async Task<IActionResult> CheckCanSendMessage(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest(new { error = "Invalid phone number provided" });
            }

            try
            {
                var canSend = await _rateLimiter.CanSendMessageAsync(phoneNumber);
                return Ok(new { canSend });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid phone number provided");
                return BadRequest(new { error = "Invalid phone number provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("record/{phoneNumber}")]
        public async Task<IActionResult> RecordMessageSent(string phoneNumber)
        {
            try
            {
                await _rateLimiter.RecordMessageSentAsync(phoneNumber);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid phone number provided");
                return BadRequest(new { error = "Invalid phone number provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording message");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("cleanup")]
        public async Task<IActionResult> TriggerCleanup()
        {
            try
            {
                await _rateLimiter.CleanupInactiveNumbersAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}