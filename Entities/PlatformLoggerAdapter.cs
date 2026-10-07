using Microsoft.Extensions.Logging;
using Monitel.PlatformInfrastructure.Logger;
using System;

namespace sapphire_diffmaker.Entities
{
    public class PlatformLoggerAdapter : IPlatformLogger
    {
        private readonly ILogger _logger;

        public PlatformLoggerAdapter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LogPriority MinPriority { get; set; } = LogPriority.Lowest;

        public bool IsEnable => true;

        public void SetHdrString(string key, string value)
        {
            _logger.LogDebug("SetHdrString: {Key} = {Value}", key, value);
        }

        public void Write(LogCategory category, LogPriority priority, string message, params object[] args)
        {
            Write(category, priority, false, message, args);
        }

        public void Write(LogCategory category, LogPriority priority, bool system, string message, params object[] args)
        {
            if (!IsEnable)
                return;

            if (priority > MinPriority)
                return;

            string str = (args.Length != 0) ? string.Format(message, args) : message;
            string fullStr = $"[Monitel InnerLog] {str}";
            switch (category)
            {
                case LogCategory.Error:
                    _logger.LogError(fullStr);
                    break;
                case LogCategory.Warn:
                    _logger.LogWarning(fullStr);
                    break;
                case LogCategory.Info:
                    _logger.LogInformation(fullStr);
                    break;
            }
        }
    }
}

