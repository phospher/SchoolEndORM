namespace ORMFramework.Statment;

public sealed record QueryExpression(List<StatElement> NifixExpression, List<StatElement> SuffixExpression, string Expression);
