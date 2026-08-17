using System.Buffers;
using System.Globalization;
using System.Text;

namespace ORMFramework.Statment;
public class StatementAnalyst
{
    private List<StatElement> _expression;
    private List<StatElement> _suffixExpression;
    private StringBuilder _currentElement;
    private char[] _queryString;
    private int _stringIndex;
    private int _expressionIndex;
    private static readonly SearchValues<char> _operatorChars = SearchValues.Create("=><!&|like()");

    // `Operator` has 12 values but this table is 10×10; LeftParenthesis(10)/RightParenthesis(11)
    // are diverted by ProcessOperatorStack early — do not enlarge the table, do not reorder the enum.
    private readonly int[,] _priorityTable = new int[,]
    {
        // Equals (0)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
        // UnEquals (1)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
        // Greater (2)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
        // GreaterEquals (3)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
        // Less (4)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
        // LessEquals (5)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
        // And (6)
        { -1, -1, -1, -1, -1, -1, 0, 1, -1, -1 },
        // Or (7)
        { -1, -1, -1, -1, -1, -1, -1, 0, -1, -1 },
        // Not (8)
        { -1, -1, -1, -1, -1, -1, 1, 1, 0, -1 },
        // Like (9)
        { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }
    };

    public string QueryString
    {
        get { return new string(_queryString); }
        set
        {
            if (string.IsNullOrEmpty(value.Trim()))
            {
                throw new Exception("QueryString is empty");
            }
            _queryString = value.ToCharArray();
            Init();
        }
    }

    public StatementAnalyst(string queryString)
    {
        _expression = new List<StatElement>();
        _currentElement = new StringBuilder();
        _suffixExpression = new List<StatElement>();
        QueryString = queryString;
        Init();
    }

    public QueryExpression GetQueryExpression()
    {
        return new QueryExpression(_expression, _suffixExpression, new String(_queryString));
    }

    private void Init()
    {
        if (_queryString == null || _queryString.Length == 0)
        {
            throw new Exception("QueryString is empty");
        }
        if (_expression.Count > 0)
        {
            _expression.Clear();
        }
        if (_currentElement.Length > 0)
        {
            _currentElement.Clear();
        }
        if (_suffixExpression.Count > 0)
        {
            _suffixExpression.Clear();
        }
        _stringIndex = 0;
        _expressionIndex = 0;
        ProcessAnalysis();
    }

    private void ProcessAnalysis()
    {
        ReadElement();
        GenerateSuffixExpression();
    }

    #region Lexical Analysis

    private static bool IsVariableFirstChar(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsVariableChar(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';

    private bool IsOperatorChar(char c) => _operatorChars.Contains(c);

    private StatElement CreateElement(StatElementType type)
    {
        if (_currentElement.Length > 0)
        {
            var element = new StatElement();
            if (type == StatElementType.Variable)
            {
                switch (_currentElement.ToString().ToLowerInvariant())
                {
                    case "true":
                        element.Type = StatElementType.Constant;
                        element.Value = true;
                        break;
                    case "false":
                        element.Type = StatElementType.Constant;
                        element.Value = false;
                        break;
                    case "like":
                        element.Type = StatElementType.Operator;
                        element.Value = Operator.Like;
                        break;
                    case "null":
                        element.Type = StatElementType.Constant;
                        element.Value = "null";
                        break;
                    default:
                        element.Type = StatElementType.Variable;
                        element.Value = _currentElement.ToString();
                        break;
                }
            }
            else if (type == StatElementType.Operator)
            {
                element.Type = StatElementType.Operator;
                element.Value = GetOperatorByString(_currentElement.ToString());
            }
            else
            {
                string text = _currentElement.ToString();
                if (text is not ['\'', .., '\''] && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    throw new Exception($"Invalid Vairable:{text}");
                }
                element.Type = StatElementType.Constant;
                element.Value = text;
            }
            _currentElement.Clear();
            return element;
        }
        else
        {
            return null;
        }
    }

    private static Operator GetOperatorByString(string operatorStr) => operatorStr.ToLowerInvariant() switch
    {
        "==" => Operator.Equals,
        "!=" => Operator.UnEquals,
        ">" => Operator.Greater,
        ">=" => Operator.GreaterEquals,
        "<" => Operator.Less,
        "<=" => Operator.LessEquals,
        "&&" => Operator.And,
        "||" => Operator.Or,
        "!" => Operator.Not,
        "like" => Operator.Like,
        "(" => Operator.LeftParenthesis,
        ")" => Operator.RightParenthesis,
        _ => throw new Exception($"Invaild Operator:{operatorStr}")
    };

    private void ReadElement()
    {
        while (_stringIndex < _queryString.Length)
        {
            char currentChar = _queryString[_stringIndex];
            while (currentChar == ' ')
            {
                currentChar = _queryString[++_stringIndex];
            }
            if (IsVariableFirstChar(currentChar))
            {
                ReadVariableChar();
            }
            else if (IsOperatorChar(currentChar))
            {
                ReadOperatorChar();
            }
            else
            {
                ReadConstantChar();
            }
        }
    }

    private void ReadVariableChar()
    {
        char currentChar = _queryString[_stringIndex];
        while (_stringIndex < _queryString.Length)
        {
            if (IsVariableChar(currentChar))
            {
                _currentElement.Append(currentChar);
                _stringIndex++;
                if (_stringIndex >= _queryString.Length)
                {
                    break;
                }
                else
                {
                    currentChar = _queryString[_stringIndex];
                }
            }
            else
            {
                break;
            }
        }
        StatElement element = CreateElement(StatElementType.Variable);
        if (element != null)
        {
            _expression.Add(element);
        }
    }

    private void ReadOperatorChar()
    {
        char currentChar = _queryString[_stringIndex];
        _currentElement.Append(currentChar);
        if (currentChar == '!' || currentChar == '<' || currentChar == '>')
        {
            currentChar = _queryString[++_stringIndex];
            if (currentChar == '=')
            {
                _currentElement.Append(currentChar);
                _stringIndex++;
            }
        }
        else if (currentChar == ')' || currentChar == '(')
        {
            _stringIndex++;
        }
        else
        {
            _currentElement.Append(_queryString[++_stringIndex]);
            _stringIndex++;
        }
        StatElement element = CreateElement(StatElementType.Operator);
        if (element != null)
        {
            _expression.Add(element);
        }
    }

    private void ReadConstantChar()
    {
        char currentChar = _queryString[_stringIndex];
        if (currentChar == '\'')
        {
            _currentElement.Append(currentChar);
            while ((currentChar = _queryString[++_stringIndex]) != '\'')
            {
                if (_stringIndex < _queryString.Length - 1)
                {
                    _currentElement.Append(currentChar);
                }
                else
                {
                    throw new Exception("' is expected");
                }
            }
            _currentElement.Append(currentChar);
            _stringIndex++;
        }
        else
        {
            while (!IsOperatorChar(currentChar))
            {
                _currentElement.Append(currentChar);
                _stringIndex++;
                if (_stringIndex >= _queryString.Length)
                {
                    break;
                }
                else
                {
                    currentChar = _queryString[_stringIndex];
                }
            }
        }
        StatElement element = CreateElement(StatElementType.Constant);
        if (element != null)
        {
            _expression.Add(element);
        }
    }

    #endregion

    #region Syntax Analysis

    private void GenerateSuffixExpression()
    {
        StatElement element = _expression[_expressionIndex];
        var operatorStack = new Stack<StatElement>();
        if (element.Type == StatElementType.Constant || element.Type == StatElementType.Variable)
        {
            if (!ReadVariableOrConstant())
            {
                throw new Exception("Invalid Statement");
            }
        }
        else if (element is { Type: StatElementType.Operator, Value: Operator.Not })
        {
            if (!ReadNotOperation(operatorStack))
            {
                throw new Exception("Invalid Statement");
            }
        }
        else if (element is { Type: StatElementType.Operator, Value: Operator.LeftParenthesis })
        {
            if (!ReadLeftParenthesis(operatorStack))
            {
                throw new Exception("Invalid Statement");
            }
        }
        while (_expressionIndex < _expression.Count)
        {
            element = _expression[_expressionIndex];
            if (element.Type == StatElementType.Constant || element.Type == StatElementType.Variable)
            {
                if (!ReadVariableOrConstant())
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else if (element is { Type: StatElementType.Operator, Value: Operator.Not })
            {
                if (!ReadNotOperation(operatorStack))
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else if (element is { Type: StatElementType.Operator, Value: Operator.LeftParenthesis })
            {
                if (!ReadLeftParenthesis(operatorStack))
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else if (element is { Type: StatElementType.Operator, Value: Operator.RightParenthesis })
            {
                if (!ReadRightParenthesis(operatorStack))
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else
            {
                if (!ReadOperator(operatorStack))
                {
                    throw new Exception("Invalid Statement");
                }
            }
        }
        ClearOperatorStack(operatorStack);
    }

    private bool ReadNotOperation(Stack<StatElement> operatorStack)
    {
        StatElement element = _expression[_expressionIndex++];
        ProcessOperatorStack(element, operatorStack);
        if (_expressionIndex == _expression.Count)
        {
            return false;
        }
        element = _expression[_expressionIndex];
        return element is { Type: StatElementType.Operator, Value: Operator.LeftParenthesis };
    }

    private bool ReadLeftParenthesis(Stack<StatElement> opeartorStack)
    {
        StatElement element = _expression[_expressionIndex++];
        ProcessOperatorStack(element, opeartorStack);
        if (_expressionIndex == _expression.Count)
        {
            return false;
        }
        element = _expression[_expressionIndex];
        return element is { Type: StatElementType.Operator, Value: Operator.Not or Operator.LeftParenthesis }
            || element.Type == StatElementType.Variable
            || element.Type == StatElementType.Constant;
    }

    private bool ReadRightParenthesis(Stack<StatElement> operatorStack)
    {
        StatElement element = _expression[_expressionIndex++];
        ProcessOperatorStack(element, operatorStack);
        if (_expressionIndex == _expression.Count)
        {
            return true;
        }
        element = _expression[_expressionIndex];
        return element is { Type: StatElementType.Operator, Value: not (Operator.LeftParenthesis or Operator.Not) };
    }

    private bool ReadVariableOrConstant()
    {
        StatElement element = _expression[_expressionIndex++];
        _suffixExpression.Add(element);
        if (_expressionIndex == _expression.Count)
        {
            _expressionIndex++;
            return true;
        }
        element = _expression[_expressionIndex];
        return element is { Type: StatElementType.Operator, Value: Operator.RightParenthesis }
            || element is { Type: StatElementType.Operator, Value: not (Operator.Not or Operator.LeftParenthesis) };
    }

    private bool ReadOperator(Stack<StatElement> operatorStack)
    {
        StatElement element = _expression[_expressionIndex++];
        ProcessOperatorStack(element, operatorStack);
        if (_expressionIndex == _expression.Count)
        {
            return false;
        }
        element = _expression[_expressionIndex];
        return element.Type == StatElementType.Variable
            || element.Type == StatElementType.Constant
            || element is { Type: StatElementType.Operator, Value: Operator.LeftParenthesis }
            || element is { Type: StatElementType.Operator, Value: Operator.Not };
    }

    private void ProcessOperatorStack(StatElement element, Stack<StatElement> operatorStack)
    {
        if (element.Type != StatElementType.Operator)
        {
            throw new Exception("element.Type must be StatElement.Operator");
        }

        StatElement top;
        if (operatorStack.Count == 0)
        {
            if ((Operator)element.Value == Operator.RightParenthesis)
            {
                throw new Exception("( is expected");
            }
            else
            {
                operatorStack.Push(element);
            }
        }
        else if ((Operator)element.Value == Operator.LeftParenthesis)
        {
            operatorStack.Push(element);
        }
        else if ((Operator)element.Value == Operator.RightParenthesis)
        {
            top = operatorStack.Pop();
            while ((Operator)top.Value != Operator.LeftParenthesis)
            {
                if (operatorStack.Count == 0)
                {
                    throw new Exception("( is expected");
                }
                _suffixExpression.Add(top);
                top = operatorStack.Pop();
            }
        }
        else
        {
            while (operatorStack.Count > 0)
            {
                top = operatorStack.Peek();
                if ((Operator)top.Value == Operator.LeftParenthesis)
                {
                    break;
                }
                if (_priorityTable[(int)element.Value, (int)top.Value] == 1)
                {
                    break;
                }
                _suffixExpression.Add(operatorStack.Pop());
            }
            operatorStack.Push(element);
        }
    }

    private void ClearOperatorStack(Stack<StatElement> operatorStack)
    {
        StatElement element;
        while (operatorStack.Count > 0)
        {
            element = operatorStack.Pop();
            if ((Operator)element.Value != Operator.LeftParenthesis)
            {
                _suffixExpression.Add(element);
            }
            else
            {
                throw new Exception(") is expected");
            }
        }
    }

    #endregion
}