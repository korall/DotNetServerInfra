
namespace Utility.Log
{
    public enum eLogLevel
    {
        LOG_VERBOSE = 1,
        LOG_INFO,
        LOG_DEBUG,
        LOG_WARN,
        LOG_ERROR
    }


    public interface IDILogProvider
    {
        void Write(eLogLevel logLevel, string logTag, string logFormatStr, params object[] args);
    }
}
