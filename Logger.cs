using System;
using System.IO;

namespace PowerModes
{
    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR
    }

    public static class Logger
    {
        private static readonly string logFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "logs.txt"
        );

        static Logger()
        {
            // Ensure log directory exists
            try
            {
                string logDir = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            catch
            {
                // If we can't create the log directory, we'll just skip logging to file
            }
        }

        public static void Log(LogLevel level, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] [{level}] {message}";

            // Write to console (debug)
            System.Diagnostics.Debug.WriteLine(logMessage);

            // Write to file
            try
            {
                lock (logFilePath)
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if we can't write to file
            }
        }

        public static void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.WARNING, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.ERROR, message);
        }

        public static void Error(string message, Exception ex)
        {
            Log(LogLevel.ERROR, $"{message} | Exception: {ex.GetType().Name} - {ex.Message}");
        }
    }
}
