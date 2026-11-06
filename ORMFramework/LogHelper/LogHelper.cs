using log4net;
using log4net.Config;
using log4net.Repository;

namespace ORMFramework.LogHelper;

public static class LogHelper
{
    private static ILog _log = null!;

    public static void InitLog(string configFilePath)
    {
        if (string.IsNullOrEmpty(configFilePath))
        {
            configFilePath = Path.Combine(Environment.CurrentDirectory, "Config.xml");
        }
        ILoggerRepository repository = LogManager.CreateRepository("NETCoreRepository");
        XmlConfigurator.Configure(repository, new FileInfo(configFilePath));
        _log = LogManager.GetLogger(repository.Name, "DefaultLogger");
    }

    public static void Debug(string message)
    {
        _log.Debug(message);
    }

    public static void Error(string message)
    {
        _log.Error(message);
    }

    public static void Fatal(string message)
    {
        _log.Fatal(message);
    }

    public static void Warn(string message)
    {
        _log.Warn(message);
    }

    public static void Info(string message)
    {
        _log.Info(message);
    }
}