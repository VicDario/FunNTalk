using FunNTalk.API.Hubs;
using Microsoft.AspNetCore.Builder;

namespace FunNTalk.API.Extensions;

public static class ApplicationExtension
{
    public static void ConfigureDomain(this WebApplication app)
    {
        app.UseRouting();
        app.UseRateLimiter();
        app.MapControllers();
        app.MapHub<CommunicationHub>("/communicationHub");
    }
}
