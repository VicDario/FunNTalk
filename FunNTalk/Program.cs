using FunNTalk.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.LoggingConfigure();
builder.Services.AppConfigure(builder.Configuration);

var application = builder.Build();

application.AppConfigure();

application.Run();