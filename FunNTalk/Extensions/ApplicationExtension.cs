using FunNTalk.API.Hubs;

namespace FunNTalk.Extensions;

public static class ApplicationExtension
{
    public static void AppConfigure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.UseCors();
        app.MapControllers();
        app.MapHub<CommunicationHub>("/communicationHub");
    }
}
