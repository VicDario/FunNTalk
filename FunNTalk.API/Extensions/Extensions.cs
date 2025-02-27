using Microsoft.Extensions.DependencyInjection;

namespace FunNTalk.API.Extensions;

public static class Extensions
{
    public static void ApiConfigure(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.ConfigureSignalR();
        services.AddLogging();
        services.ConfigureCors();
    }
}
