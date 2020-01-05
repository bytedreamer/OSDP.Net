using log4net.Appender;
using log4net.Core;

namespace Console
{
    public class CustomAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent.Level > Level.Debug)
            {
                Program.AddLogMessage(RenderLoggingEvent(loggingEvent));
            }
        }
    }
}