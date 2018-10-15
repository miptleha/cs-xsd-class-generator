using System;
using System.Collections.Generic;

namespace Log
{
    /// <summary>
    /// Logging all actions for application.
    /// Add code for logging to database, console, etc.
    /// Log4net requires initialization, see App.config.
    /// </summary>
    public class Logger : ILog
    {
        public Logger(Type type)
        {
            _type = type;
            log_text = log4net.LogManager.GetLogger(type);
        }

        Type _type;
        log4net.ILog log_text;

        public void Error(Exception ex)
        {
            Error("!!!Error", ex);
        }

        public void Error(string message, Exception ex)
        {
            log_text.Error(message, ex);
            Console.WriteLine("{0}\n{1}", message, ex.ToString());
        }

        public void Debug(string message)
        {
            log_text.Debug(message);
            Console.WriteLine(message);
        }
    }
}
