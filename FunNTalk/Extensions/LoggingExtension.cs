namespace FunNTalk.Extensions;

public static class LoggingExtension
{
    public static void LoggingConfigure(this ILoggingBuilder logging)
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    }
}
