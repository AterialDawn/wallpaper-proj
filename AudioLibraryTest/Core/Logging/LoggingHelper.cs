using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace player.Core.Logging
{
    public static class Logger
    {
        #region Static Variables
        public static bool LogToFileEnabled { get { return _LogToFileEnabled; } set { if (!_FileLoggingInitialized) { throw new InvalidOperationException("File logging not initialized."); } _LogToFileEnabled = value; } }
        public static bool LogToConsoleEnabled { get; set; } = true;
        public static event EventHandler<MessageLoggedEventArgs> MessageLogged;

        private static string LogFilePath;
        private static bool Enabled = false;
        private static bool _FileLoggingInitialized = false;
        private static bool _LogToFileEnabled = false;
        private static AsyncCollection<string> loggingQueue = new AsyncCollection<string>();
        private static Task loggingTask = null;
        private static string currentLogFile = null;
        private static FileStream currentLogStream = null;
        private static StreamWriter streamWriter = null;
        #endregion

        #region Public Static methods
        public static void Log(string message, params object[] args)
        {
            if (!Enabled) throw new InvalidOperationException("Logging has not been enabled. Call InitLogging before any other methods.");

            loggingQueue.Add(string.Format(message, args));
        }

        public static void Log(string message)
        {
            if (!Enabled) throw new InvalidOperationException("Logging has not been enabled. Call InitLogging before any other methods.");

            loggingQueue.Add(message);
        }

        public static string GetCurrentLogFilePath()
        {
            return Path.Combine(LogFilePath, string.Format("{0:yyyy-MM-dd}.log", DateTime.Now));
        }

        public static void InitLogging(string LoggingPath)
        {
            if (Enabled) throw new InvalidOperationException("Attempting to initialize logging multiple times.");
            LogFilePath = LoggingPath;
            if (!Directory.Exists(LogFilePath))
            {
                try
                {
                    Directory.CreateDirectory(LogFilePath);
                }
                catch (Exception e)
                {
                    throw new UnauthorizedAccessException("Unable to create logging directory.", e);
                }
            }
            _FileLoggingInitialized = true;
            _LogToFileEnabled = true;

            loggingTask = MonitorLogging();

            Enabled = true;
        }

        public static void InitLoggingNoFile()
        {
            if (Enabled) throw new InvalidOperationException("Attempt to initialize logging multiple times.");
            LogToFileEnabled = false;
            _FileLoggingInitialized = false;
            LogFilePath = null;

            loggingTask = MonitorLogging();

            Enabled = true;
        }

        public static void DisableLogging()
        {
            if (!Enabled) throw new InvalidOperationException("Attempting to disable logging without a previous call to init.");
            LogFilePath = null;
            loggingQueue.CompleteAdding();
            Enabled = false;
            if (currentLogStream != null)
            {
                currentLogStream.Dispose();
                currentLogStream = null;
            }
            if (streamWriter != null)
            {
                streamWriter.Dispose();
                streamWriter = null;
            }
        }


        #endregion

        #region Private Static methods
        private static async Task MonitorLogging()
        {
            while (await loggingQueue.OutputAvailableAsync())
            {
                string loggingMessage = await loggingQueue.TakeAsync();
                bool consoleLogged = false, fileLogged = false;

                if (LogToFileEnabled)
                {
                    LogToFile(loggingMessage);
                    fileLogged = true;
                }
                if (LogToConsoleEnabled)
                {
                    LogToConsole(loggingMessage);
                    consoleLogged = true;
                }

                MessageLogged?.Invoke(null, new MessageLoggedEventArgs(loggingMessage, consoleLogged, fileLogged));
            }
        }

        private static void LogToConsole(string Message)
        {
            Console.WriteLine(Message);
        }

        private static void LogToFile(string Message)
        {
            try
            {
                ReinitializeFileIfNeeded();
                streamWriter.WriteLine(Message);
            }
            catch (Exception e)
            {
                LogToConsole("Exception caught writing to log.\n" + e.ToString());
            }
        }

        private static void ReinitializeFileIfNeeded()
        {
            if (currentLogFile == null || currentLogStream == null || currentLogFile != GetCurrentLogFilePath())
            {
                currentLogFile = GetCurrentLogFilePath();
                if (currentLogStream != null) currentLogStream.Dispose();
                if (streamWriter != null) streamWriter.Dispose();
                currentLogStream = new FileStream(currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                streamWriter = new StreamWriter(currentLogStream);
                streamWriter.AutoFlush = true;
            }
            
        }

        private static string GetTime()
        {
            return DateTime.Now.ToLongTimeString();
        }
        #endregion
        
    }
}
