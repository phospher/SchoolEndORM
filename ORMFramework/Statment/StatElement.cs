namespace ORMFramework.Statment;

public class StatElement
{
    public object Value { get; set; } = "";

    public StatElementType Type { get; set; }
}

public enum StatElementType
{
    Variable,
    Constant,
    Operator,
    Result
}