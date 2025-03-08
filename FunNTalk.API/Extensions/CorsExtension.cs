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
                    builder.WithOrigins("https://fun-n-talk-front.vercel.app")
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod();

                    builder.WithOrigins("https://funntalk.vicdario.com")
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod();
                }
            );
        });
    }
}
