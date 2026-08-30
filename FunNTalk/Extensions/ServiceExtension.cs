using FunNTalk.API.Extensions;
using FunNTalk.Infrastructure.Extensions;

namespace FunNTalk.Extensions;

public static class ServiceExtension
{
    public static void AppConfigure(this IServiceCollection services, IConfiguration configuration)
    {
        services.ApiConfigure();
        services.InfrastructureConfigure(configuration);
    }
}