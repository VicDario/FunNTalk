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
                {
                    builder.WithOrigins("https://127.0.0.1:4200")
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod();
                });
        });
    }
}
