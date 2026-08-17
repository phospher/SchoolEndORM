using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ORMFramework.Exceptions;
using ORMFramework.Statment;

namespace ORMFramework.Cache;
public static class ObjectCalculator
{

    #region IsTargetObject

    public static bool IsTargetObject(object @object, QueryExpression expression)
    {
        List<StatElement> suffixExpression = expression.SuffixExpression;
        var resultStack = new Stack<StatElement>();
        StatElement result;
        foreach (StatElement element in suffixExpression)
        {
            switch (element.Type)
            {
                case StatElementType.Constant:
                    resultStack.Push(element);
                    break;
                case StatElementType.Operator:
                    ProcessOperator((Operator)element.Value, resultStack, @object);
                    break;
                case StatElementType.Variable:
                    ProcessVariable(@object, element, resultStack);
                    break;
                default:
                    throw new QuerySyntaxException("Invalid Statement");
            }
        }
        if (resultStack.Count == 1)
        {
            result = resultStack.Pop();
            if ((result.Type == StatElementType.Result || result.Type == StatElementType.Constant) && result.Value is bool b)
            {
                return b;
            }
            else
            {
                throw new QuerySyntaxException("Invalid Statement");
            }
        }
        else
        {
            throw new QuerySyntaxException("Invalid Statement");
        }
    }

    private static void ProcessVariable(object @object, StatElement element, Stack<StatElement> resultStack)
    {
        if (element.Value is string propertyName && @object.GetType().GetProperty(propertyName) != null)
        {
            resultStack.Push(element);
        }
        else
        {
            throw new QuerySyntaxException($"{@object.GetType()} do not contain the property:{element.Value}");
        }
    }

    private static void ProcessOperator(Operator @operator, Stack<StatElement> resultStack, object @object)
    {
        if (@operator == Operator.Not)
        {
            ProcessNotOperator(resultStack, @object);
        }
        else if (IsBooleanOperator(@operator))
        {
            ProcessBooleanOperator(resultStack, @operator, @object);
        }
        else
        {
            ProcessNotBooleanOperator(resultStack, @operator, @object);
        }
    }

    private static bool IsBooleanOperator(Operator @operator) => @operator == Operator.And || @operator == Operator.Not || @operator == Operator.Or;

    private static void ProcessNotOperator(Stack<StatElement> resultStack, object @object)
    {
        if (resultStack.Count == 0)
        {
            throw new QuerySyntaxException("Invalid Statement");
        }

        StatElement element = resultStack.Pop();
        if (element.Type == StatElementType.Result)
        {
            element.Value = !((bool)element.Value);
        }
        else if (element.Type == StatElementType.Variable)
        {
            PropertyInfo property = GetObjectProperty(@object, (string)element.Value);
            if (property.GetValue(@object) is bool b)
            {
                element = new StatElement();
                element.Type = StatElementType.Result;
                element.Value = !b;
            }
            else
            {
                throw new QuerySyntaxException($"{@object.GetType()}.{property.Name} is not a boolean property");
            }
        }
        else
        {
            if (element.Value is bool value)
            {
                element = new StatElement();
                element.Type = StatElementType.Result;
                element.Value = !value;
            }
            else
            {
                throw new QuerySyntaxException($"{element.Value} is not boolean");
            }
        }
        resultStack.Push(element);
    }

    private static void ProcessBooleanOperator(Stack<StatElement> resultStack, Operator @operator, object @object)
    {
        if (resultStack.Count < 2)
        {
            throw new QuerySyntaxException("Invalid Statement");
        }

        StatElement y = resultStack.Pop();
        StatElement x = resultStack.Pop();
        if (x.Type == StatElementType.Variable)
        {
            GetObjectProperty(@object, (string)x.Value);
        }
        if (y.Type == StatElementType.Variable)
        {
            GetObjectProperty(@object, (string)y.Value);
        }
        if (x.Value is bool xb && y.Value is bool yb)
        {
            var element = new StatElement();
            element.Type = StatElementType.Result;
            element.Value = CalculateBooleanOperator(xb, yb, @operator);
            resultStack.Push(element);
        }
        else
        {
            throw new QuerySyntaxException("Invalid Statement");
        }
    }

    private static void ProcessNotBooleanOperator(Stack<StatElement> resultStack, Operator @operator, object @object)
    {
        if (resultStack.Count < 2)
        {
            throw new QuerySyntaxException("Invalid Statement");
        }

        StatElement y = resultStack.Pop();
        StatElement x = resultStack.Pop();
        var element = new StatElement();
        element.Type = StatElementType.Result;
        object? xValue = x.Type == StatElementType.Variable
            ? GetObjectProperty(@object, (string)x.Value).GetValue(@object)
            : x.Value;
        object? yValue = y.Type == StatElementType.Variable
            ? GetObjectProperty(@object, (string)y.Value).GetValue(@object)
            : y.Value;
        if (IsString(x, @object) && IsString(y, @object))
        {
            element.Value = CalculateStringOperator(x, y, @operator, @object);
        }
        else if (!(IsString(x, @object) || IsString(y, @object)))
        {
            element.Value = CalculateNotBooleanOperator(Convert.ToDouble(xValue, CultureInfo.InvariantCulture), Convert.ToDouble(yValue, CultureInfo.InvariantCulture), @operator);
        }
        else
        {
            throw new QuerySyntaxException("Invalid Statment");
        }
        resultStack.Push(element);
    }

    private static bool CalculateStringOperator(StatElement x, StatElement y, Operator @operator, object @object)
    {
        string xStr;
        string yStr;
        if (x.Type == StatElementType.Variable)
        {
            PropertyInfo property = GetObjectProperty(@object, (string)x.Value);
            if (property.GetValue(@object) is DateTime xDateTime)
            {
                xStr = FormattableString.Invariant($"'{xDateTime:yyyy-MM-dd HH:mm:ss}'");
            }
            else
            {
                xStr = (string)property.GetValue(@object)!;
            }
        }
        else
        {
            xStr = x.Value.ToString()!;
        }
        if (y.Type == StatElementType.Variable)
        {
            PropertyInfo property = GetObjectProperty(@object, (string)y.Value);
            if (property.GetValue(@object) is DateTime yDateTime)
            {
                yStr = FormattableString.Invariant($"'{yDateTime:yyyy-MM-dd HH:mm:ss}'");
            }
            else
            {
                yStr = (string)property.GetValue(@object)!;
            }
        }
        else
        {
            yStr = y.Value.ToString()!;
        }
        if (xStr is ['\'', .., '\''])
        {
            xStr = xStr[1..^1];
        }
        if (yStr is ['\'', .., '\''])
        {
            yStr = yStr[1..^1];
        }
        return @operator switch
        {
            Operator.Equals => xStr == yStr,
            Operator.UnEquals => xStr != yStr,
            Operator.Like => CalculateLikeOperator(xStr, yStr, x.Type, y.Type),
            _ => throw new QuerySyntaxException("Invalid Statement"),
        };
    }

    private static bool CalculateLikeOperator(string x, string y, StatElementType xType, StatElementType yType)
    {
        if (yType == StatElementType.Constant)
        {
            int startIndex = 0;
            int endIndex;
            StringBuilder patten = new StringBuilder();
            Regex patternRegex = new Regex(@"\[(\S|\s)*]");
            Match pattenMatch = patternRegex.Match(y);
            while (pattenMatch.Success)
            {
                endIndex = pattenMatch.Index;
                patten.Append(y.Substring(startIndex, endIndex - startIndex).Replace("%", @"\w*").Replace("_", @"\w"));
                patten.Append(pattenMatch.Value);
                startIndex = pattenMatch.Index + pattenMatch.Length;
                pattenMatch = pattenMatch.NextMatch();
            }
            endIndex = y.Length;
            if (startIndex < endIndex)
            {
                patten.Append(y.Substring(startIndex, endIndex - startIndex).Replace("%", @"\w*").Replace("_", @"\w"));
            }
            patten.Insert(0, '^');
            patten.Append('$');
            Regex regex = new Regex(patten.ToString());
            return regex.IsMatch(x);
        }
        else
        {
            throw new QuerySyntaxException("Invalid Statement");
        }
    }

    private static bool CalculateBooleanOperator(bool x, bool y, Operator @operator) => @operator switch
    {
        Operator.And => x && y,
        Operator.Or => x || y,
        _ => throw new QuerySyntaxException("Invalid operator"),
    };

    private static bool CalculateNotBooleanOperator(double x, double y, Operator @operator) => @operator switch
    {
        Operator.Equals => x == y,
        Operator.Greater => x > y,
        Operator.GreaterEquals => x >= y,
        Operator.Less => x < y,
        Operator.LessEquals => x <= y,
        Operator.UnEquals => x != y,
        _ => throw new QuerySyntaxException("Invalid Statement"),
    };

    private static PropertyInfo GetObjectProperty(object @object, string propertyName)
    {
        PropertyInfo? property = @object.GetType().GetProperty(propertyName);
        if (property != null)
        {
            return property;
        }
        else
        {
            throw new QuerySyntaxException($"{@object.GetType()} do not have the property:{propertyName}");
        }
    }

    private static bool IsString(StatElement element, object @object)
    {
        if (element.Type == StatElementType.Variable && element.Value is string name)
        {
            Type type = GetObjectProperty(@object, name).PropertyType;
            return type == typeof(string) || type == typeof(DateTime) || type == typeof(char);
        }
        else if (element.Type == StatElementType.Constant)
        {
            string value = (string)element.Value;
            return value.Length > 1 && value[0] == '\'' && value[^1] == '\'';
        }
        else
        {
            throw new QuerySyntaxException("'element' must a variable or constant");
        }
    }

    #endregion
}