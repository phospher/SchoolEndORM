namespace ORMFramework.Exceptions;

public sealed class OrmConfigurationException : OrmException
{
    public OrmConfigurationException()
    {
    }

    public OrmConfigurationException(string message) : base(message)
    {
    }

    public OrmConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
