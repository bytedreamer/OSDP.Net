using log4net.Appender;
using log4net.Core;

namespace Console
{
    public class CustomAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            Program.AddLogMessage(RenderLoggingEvent(loggingEvent));
        }
    }
}