using FunNTalk.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.LoggingConfigure();
builder.Services.AppConfigure();

var application = builder.Build();

application.AppConfigure();

application.Run();