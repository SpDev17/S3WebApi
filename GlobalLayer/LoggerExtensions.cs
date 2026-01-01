using System.Runtime.CompilerServices;
using ILogger = Serilog.ILogger;

namespace S3WebApi;

public static class LoggerExtensions
{
    public static ILogger AddMethodName(this ILogger logger, [CallerMemberName] string memberName = "")//for adding method name and to be added in information
    {
        return logger
            .ForContext("MemberName", memberName);
    }
}
