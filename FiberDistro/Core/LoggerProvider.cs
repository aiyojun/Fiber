using Microsoft.Extensions.Logging;

namespace FiberDistro.Core;

public static class LoggerProvider
{
    static LoggerProvider()
    {
        _logger = LoggerFactory
                .Create(builder => 
                    builder
                        .AddConsole(options => options.FormatterName = "simple")
                        .AddSimpleConsole(options =>
                        {
                            options.SingleLine = true;
                            options.TimestampFormat = "[HH:mm:ss] ";
                            options.IncludeScopes = true;
                        })
                        .SetMinimumLevel(LogLevel.Debug)
                )
                .CreateLogger("Fiber")
            ;
    }

    public static ILogger Logger => _logger;

    private static ILogger _logger;
}