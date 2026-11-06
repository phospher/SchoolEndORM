namespace ORMFramework.Exceptions;

public sealed class QuerySyntaxException : OrmException
{
    public QuerySyntaxException()
    {
    }

    public QuerySyntaxException(string message) : base(message)
    {
    }

    public QuerySyntaxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
