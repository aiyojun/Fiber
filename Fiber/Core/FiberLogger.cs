using Microsoft.Extensions.Logging;

namespace Fiber.Core;

public static class FiberLogger
{
    public static ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Fiber");
}