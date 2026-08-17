namespace ORMFramework.Statment;
public class QueryExpression
{
    private List<StatElement> _nifixExpression;
    private List<StatElement> _suffixExpression;
    private string _expression;

    public List<StatElement> NifixExpression
    {
        get { return _nifixExpression; }
    }

    public List<StatElement> SuffixExpression
    {
        get { return _suffixExpression; }
    }

    public string Expression
    {
        get { return _expression; }
    }

    internal QueryExpression(List<StatElement> nifixExpression, List<StatElement> suffixExpression, string expression)
    {
        _nifixExpression = nifixExpression;
        _suffixExpression = suffixExpression;
        _expression = expression;
    }
}