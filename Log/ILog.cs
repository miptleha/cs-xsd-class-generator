using System;

namespace Log
{
    /// <summary>
    /// This interface must be implemented only by Logger class
    /// </summary>
    interface ILog
    {
        void Error(Exception ex);
        void Error(string message, Exception ex);
        void Debug(string message);
    }
}
