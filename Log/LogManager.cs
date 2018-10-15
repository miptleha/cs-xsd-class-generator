using System;

namespace Log
{
    /// <summary>
    /// Helper class for log creation.
    /// Sample usage: ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    /// </summary>
    class LogManager
    {
        public static ILog GetLogger(Type type)
        {
            var log = new Logger(type);
            return log;
        }
    }
}
