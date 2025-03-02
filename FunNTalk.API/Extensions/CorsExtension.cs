using Microsoft.Extensions.DependencyInjection;

namespace FunNTalk.API.Extensions;

public static class CorsExtension
{
    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                builder =>
                    builder.WithOrigins("https://fun-n-talk-1058570323303.us-central1.run.app")
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod()
            );
        });
    }
}
