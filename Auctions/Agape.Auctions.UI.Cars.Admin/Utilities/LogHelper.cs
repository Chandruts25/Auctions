using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Agape.Auctions.UI.Cars.Admin.Utilities
{
    public class LogHelper
    {
        public static IConfiguration _config { get; private set; }
        private readonly ILogger<Controller> _logger;

        public LogHelper(IConfiguration configuration, ILogger<Controller> logger)
        {
            _config = configuration;
            _logger = logger;
        }
        public void LogInformation(string message)
        {
            if (GetLogKeuyValue("EnableInformation"))
                _logger.LogInformation(message);
        }

        public void LogError(string message)
        {
            if (GetLogKeuyValue("EnableError"))
                _logger.LogError(message);
        }
        public void LogWarning(string message)
        {
            if (GetLogKeuyValue("EnableWarning"))
                _logger.LogWarning(message);
        }
        public void LogCriticalInformation(string message)
        {
            if (GetLogKeuyValue("EnableCriticalInformation"))
                _logger.LogCritical(message);
        }

        public bool GetLogKeuyValue(string key)
        {
            bool result = false;
            try
            {
                result = _config.GetSection("LogInfo").GetValue<bool>(key);
                return result;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return result;
        }
    }

    public class LogHelperComponent
    {
        public static IConfiguration _config { get; private set; }
        private readonly ILogger<ViewComponent> _logger;

        public LogHelperComponent(IConfiguration configuration, ILogger<ViewComponent> logger)
        {
            _config = configuration;
            _logger = logger;
        }
        public void LogInformation(string message)
        {
            if (GetLogKeuyValue("EnableInformation"))
                _logger.LogInformation(message);
        }

        public void LogError(string message)
        {
            if (GetLogKeuyValue("EnableError"))
                _logger.LogError(message);
        }
        public void LogWarning(string message)
        {
            if (GetLogKeuyValue("EnableWarning"))
                _logger.LogWarning(message);
        }
        public void LogCriticalInformation(string message)
        {
            if (GetLogKeuyValue("EnableCriticalInformation"))
                _logger.LogCritical(message);
        }

        public bool GetLogKeuyValue(string key)
        {
            bool result = false;
            try
            {
                result = _config.GetSection("LogInfo").GetValue<bool>(key);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return result;
        }
    }
}
