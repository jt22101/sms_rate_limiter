# SMS Rate Limiter Microservice

A .NET Core microservice that ensures SMS messages from businesses to customers don't exceed provider limits, preventing unnecessary API costs.

## Core Features
- Real-time rate limit checking for phone numbers and account-wide usage
- Automatic cleanup of inactive numbers
- High-performance request handling
- REST API endpoints for rate limit management

## API Endpoints
- `GET /api/ratelimit/check/{phoneNumber}` - Check if message can be sent
- `POST /api/ratelimit/record/{phoneNumber}` - Record a sent message
- `POST /api/ratelimit/cleanup` - Trigger cleanup of inactive numbers

## Configuration
Update rate limits in `appsettings.json`:
```json
{
  "RateLimitConfig": {
    "MaxMessagesPerNumberPerSecond": 50,
    "MaxMessagesPerAccountPerSecond": 500,
    "InactivityThresholdMinutes": 30,
    "CleanupIntervalMinutes": 5
  }
}
```

## Testing
Solution includes:
- Unit tests for core functionality
- Integration tests for API endpoints
- Load tests verifying high-volume performance

## Deployment & Scaling
See `DEPLOYMENT_PLAN.md` for detailed information on:
- Containerization and hosting strategy
- Monitoring setup
- Scaling approach
- Performance optimization

## Tech Stack
- .NET 6.0
- Docker
- Azure/Kubernetes (recommended deployment)
- Redis (for scaled deployments)

## Performance
Tested to handle:
- 200+ requests per second per endpoint
- Sub-500ms response times (99th percentile)
- Concurrent requests from multiple numbers

## Future Enhancements
- Web interface for monitoring (Angular/JavaScript)
- Enhanced metrics visualization
- Advanced filtering capabilities
