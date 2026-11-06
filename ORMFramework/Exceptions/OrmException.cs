namespace ORMFramework.Exceptions;

public class OrmException : Exception
{
    public OrmException()
    {
    }

    public OrmException(string message) : base(message)
    {
    }

    public OrmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
