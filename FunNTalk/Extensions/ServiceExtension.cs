﻿using FunNTalk.API.Extensions;
using FunNTalk.Infrastructure.Extensions;

namespace FunNTalk.Extensions;

public static class ServiceExtension
{
    public static void AppConfigure(this IServiceCollection services)
    {
        services.ApiConfigure();
        services.InfrastructureConfigure();
    }
}