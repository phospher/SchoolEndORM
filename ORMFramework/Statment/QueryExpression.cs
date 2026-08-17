namespace ORMFramework.Statment;
public class QueryExpression
{
    public List<StatElement> NifixExpression { get; }

    public List<StatElement> SuffixExpression { get; }

    public string Expression { get; }

    internal QueryExpression(List<StatElement> nifixExpression, List<StatElement> suffixExpression, string expression)
    {
        NifixExpression = nifixExpression;
        SuffixExpression = suffixExpression;
        Expression = expression;
    }
}