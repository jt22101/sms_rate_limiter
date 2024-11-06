using sms_rate_limiter.Models;
using sms_rate_limiter.Services;
using sms_rate_limiter.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

// Configure rate limiter settings
builder.Services.Configure<RateLimitConfig>(
    builder.Configuration.GetSection("RateLimitConfig"));

// Register rate limiter service as a singleton
builder.Services.AddSingleton<IRateLimiter, RateLimiterService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }