﻿using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FunNTalk.Infrastructure.Extensions;

public static class Extensions
{
    public static void InfrastructureConfigure(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<IChatRoomRepository, ChatRoomRepository>();
    }
}
