using IO.Ably;
using Serilog;

namespace TestObserver
{
    public class SerilogSink : ILoggerSink
    {
        private ILogger _logger;

        public SerilogSink() : this(Log.Logger)
        {

        }
        public SerilogSink(ILogger logger)
        {
            _logger = logger ?? Serilog.Log.Logger;
        }
        public void LogEvent(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    _logger.Debug(message);
                    break;
                case LogLevel.Warning:
                    _logger.Warning(message);
                    break;
                case LogLevel.Error:
                    _logger.Error(message);
                    break;
                default:
                    break;
            }
        }
    }
}