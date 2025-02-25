using Microsoft.Extensions.DependencyInjection;

namespace FunNTalk.API.Extensions;

public static class Extensions
{
    public static void ApiConfigure(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSignalR(signalR => signalR.EnableDetailedErrors = true);
        services.AddLogging();
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
