using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunNTalk.API.Extensions;

public static class SignalRExtension
{
        public static void ConfigureSignalR(this IServiceCollection services)
        {
            services
                .AddSignalR(signalR => signalR.EnableDetailedErrors = true)
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });
        }
    }
}
