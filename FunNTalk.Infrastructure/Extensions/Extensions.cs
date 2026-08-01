using FunNTalk.Domain.Repositories;
using FunNTalk.Domain.UseCases;
using FunNTalk.Infrastructure.Configuration;
using FunNTalk.Infrastructure.Repositories;
using FunNTalk.Infrastructure.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FunNTalk.Infrastructure.Extensions;

public static class Extensions
{
    public static void InfrastructureConfigure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IChatRoomRepository, ChatRoomRepository>();
        services.AddSingleton<IGetUsersFromRoomUseCase, GetUsersFromRoomUseCase>();

        services.Configure<IceServerOptions>(configuration.GetSection(IceServerOptions.SectionName));
        services
            .AddHttpClient<IGetIceServersUseCase, GetIceServersUseCase>(client =>
            {
                // The client waits on this before it can negotiate, so failing fast and
                // degrading to STUN beats holding the connection open.
                client.Timeout = TimeSpan.FromSeconds(5);
            });
    }
}
