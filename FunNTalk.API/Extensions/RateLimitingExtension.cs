using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FunNTalk.API.Extensions;

public static class RateLimitingExtension
{
    /// <summary>
    /// The ICE endpoint is unauthenticated, so a per-IP limit is all that stands between a
    /// script and the relay quota. It only slows harvesting down — what actually contains the
    /// damage is that the issued credentials are short lived.
    /// </summary>
    public const string IceServersPolicy = "ice-servers";

    /// <summary>
    /// The create-room endpoint is unauthenticated, so a per-IP limit is what stands between a
    /// script and mass room creation. Mirrors <see cref="IceServersPolicy"/>'s shape.
    /// </summary>
    public const string CreateRoomPolicy = "create-room";

    public static void ConfigureRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(IceServersPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.AddPolicy(CreateRoomPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }
}
