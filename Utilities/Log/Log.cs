using System;
using System.Text;

namespace Utility.Log
{
    public class Log : Singleton<Log>
    {
        IDILogProvider mLogrovider;

        public Log()
        {}

        protected override void _InitOnCreateInstance()
        {
            mLogrovider = new DebugLogProvider();
        }

        public Log(IDILogProvider iLogProvider)
        {
            mLogrovider = iLogProvider;
        }

        public void SetProvider(IDILogProvider iLogProvider)
        {
            mLogrovider = iLogProvider;
        }

        public void Write(eLogLevel logLevel, string logTag, string logFormatStr, params object[] args)
        {
            if (mLogrovider != null)
                mLogrovider.Write(logLevel, logTag, logFormatStr, args);
        }

        public void Write(eLogLevel logLevel, string logTag, string logStr)
        {
            if (mLogrovider != null)
                mLogrovider.Write(logLevel, logTag, "{0}", logStr);
        }

        public static void WriteLog(eLogLevel logLevel, string logTag, string logFormatStr, params object[] args)
        {
            if (Instance.mLogrovider != null)
                Instance.mLogrovider.Write(logLevel, logTag, logFormatStr, args);
        }

        public static void WriteLog(eLogLevel logLevel, string logTag, string logStr)
        {
            if (Instance.mLogrovider != null)
                Instance.mLogrovider.Write(logLevel, logTag, "{0}", logStr);
        }
    }

    public class DebugLogProvider : IDILogProvider
    {
        public void Write(eLogLevel logLevel, string logTag, string logFormatStr, params object[] args)
        {
            var strBuilder = new StringBuilder();
            strBuilder.AppendFormat(string.Format(logFormatStr, args));

            if (logLevel == eLogLevel.LOG_ERROR)
                System.Diagnostics.Trace.WriteLine($"{logTag}<error> {strBuilder}");
            else if (logLevel == eLogLevel.LOG_WARN)
                System.Diagnostics.Trace.WriteLine($"{logTag}<warn> {strBuilder}");
            else if (logLevel == eLogLevel.LOG_DEBUG)
                System.Diagnostics.Trace.WriteLine($"{logTag}<debug> {strBuilder}");
            else if (logLevel == eLogLevel.LOG_INFO)
                System.Diagnostics.Trace.WriteLine($"{logTag}<info> {strBuilder}");
            else if (logLevel == eLogLevel.LOG_VERBOSE)
                System.Diagnostics.Trace.WriteLine($"{logTag}<verbose> {strBuilder}");
            else
                System.Diagnostics.Trace.WriteLine($"{logTag}<lv_{logLevel}> {strBuilder}");
        }
    }

    public class Debug
    {
        // build a adhoc debug logger in case of no log provider provided
        static Log debugLog = new Log(new DebugLogProvider());

        public static void SetLogProvier(IDILogProvider logProvider)
        {
            debugLog = new Log(logProvider);
        }

        public static void LogError(string log)
        {
            debugLog.Write(eLogLevel.LOG_ERROR, "debug", "{0}", log);
        }

        public static void LogWarning(string log)
        {
            debugLog.Write(eLogLevel.LOG_WARN, "debug", "{0}", log);
        }

        public static void Log(string log)
        {
            debugLog.Write(eLogLevel.LOG_DEBUG, "debug", "{0}", log);
        }
    }
}
