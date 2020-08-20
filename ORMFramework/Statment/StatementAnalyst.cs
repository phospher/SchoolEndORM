using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Statment
{
    public class StatementAnalyst
    {
        private List<StatElement> _expression;
        private List<StatElement> _suffixExpression;
        private StringBuilder _currentElement;
        private char[] _queryString;
        private int _stringIndex;
        private int _expressionIndex;
        private readonly char[] _operatorChars = new char[] { '=', '>', '<', '!', '&', '|', 'l', 'i', 'k', 'e', '(', ')' };
        private readonly int[,] _priorityTable = new int[,] { { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }, { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }, { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }, { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }, { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }, { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }, {-1, -1, -1, -1, -1, -1, 0, 1, -1, -1 }, {-1, -1, -1, -1, -1, -1, -1, 0, -1, -1 }, {-1, -1, -1, -1, -1, -1, 1, 1, 0, -1 }, { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 }
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
                _currentElement.Remove(0, _currentElement.Length);
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

        private bool IsVariableFirstChar(char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return true;
            }
            else if (c >= 'A' && c <= 'Z')
            {
                return true;
            }
            else if (c == '_')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsVariableChar(char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return true;
            }
            else if (c >= 'A' && c <= 'Z')
            {
                return true;
            }
            else if (c >= '0' && c <= '9')
            {
                return true;
            }
            else if (c == '_')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsOperatorChar(char c)
        {
            return Array.IndexOf<char>(_operatorChars, c) != -1;
        }

        private StatElement CreateElement(StatElementType type)
        {
            if (_currentElement.Length > 0)
            {
                StatElement element = new StatElement();
                if (type == StatElementType.Variable)
                {
                    switch (_currentElement.ToString().ToLower())
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
                    if (_currentElement[0] != '\'' || _currentElement[_currentElement.Length - 1] != '\'')
                    {
                        try
                        {
                            double.Parse(_currentElement.ToString());
                        }
                        catch
                        {
                            throw new Exception(string.Format("Invalid Vairable:{0}", _currentElement.ToString()));
                        }
                    }
                    element.Type = StatElementType.Constant;
                    element.Value = _currentElement.ToString();
                }
                _currentElement.Remove(0, _currentElement.Length);
                return element;
            }
            else
            {
                return null;
            }
        }

        private Operator GetOperatorByString(string operatorStr)
        {
            switch (operatorStr.ToLower())
            {
                case "==":
                    return Operator.Equals;
                case "!=":
                    return Operator.UnEquals;
                case ">":
                    return Operator.Greater;
                case ">=":
                    return Operator.GreaterEquals;
                case "<":
                    return Operator.Less;
                case "<=":
                    return Operator.LessEquals;
                case "&&":
                    return Operator.And;
                case "||":
                    return Operator.Or;
                case "!":
                    return Operator.Not;
                case "like":
                    return Operator.Like;
                case "(":
                    return Operator.LeftParenthesis;
                case ")":
                    return Operator.RightParenthesis;
                default:
                    throw new Exception(string.Format("Invaild Operator:{0}", operatorStr));
            }
        }

        private void ReadElement()
        {
            char currentChar;
            while (_stringIndex < _queryString.Length)
            {
                currentChar = _queryString[_stringIndex];
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
            Stack<StatElement> operatorStack = new Stack<StatElement>();
            if (element.Type == StatElementType.Constant || element.Type == StatElementType.Variable)
            {
                if (!ReadVariableOrConstant())
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else if ((Operator)element.Value == Operator.Not)
            {
                if (!ReadNotOpeartion(operatorStack))
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else if ((Operator)element.Value == Operator.LeftParenthesis)
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
                else if ((Operator)element.Value == Operator.Not)
                {
                    if (!ReadNotOpeartion(operatorStack))
                    {
                        throw new Exception("Invalid Statement");
                    }
                }
                else if ((Operator)element.Value == Operator.LeftParenthesis)
                {
                    if (!ReadLeftParenthesis(operatorStack))
                    {
                        throw new Exception("Invalid Statement");
                    }
                }
                else if ((Operator)element.Value == Operator.RightParenthesis)
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

        private bool ReadNotOpeartion(Stack<StatElement> operatorStack)
        {
            StatElement element = _expression[_expressionIndex++];
            ProcessOperatorStack(element, operatorStack);
            if (_expressionIndex == _expression.Count)
            {
                return false;
            }
            element = _expression[_expressionIndex];
            if (element.Type == StatElementType.Operator && (Operator)element.Value == Operator.LeftParenthesis)
            {
                return true;
            }
            else
            {
                return false;
            }
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
            if (element.Type == StatElementType.Operator && ((Operator)element.Value == Operator.Not || (Operator)element.Value == Operator.LeftParenthesis))
            {
                return true;
            }
            else if (element.Type == StatElementType.Variable || element.Type == StatElementType.Constant)
            {
                return true;
            }
            else
            {
                return false;
            }
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
            if (element.Type == StatElementType.Operator && (Operator)element.Value != Operator.LeftParenthesis && (Operator)element.Value != Operator.Not)
            {
                return true;
            }
            else
            {
                return false;
            }
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
            if (element.Type == StatElementType.Operator && (Operator)element.Value == Operator.RightParenthesis)
            {
                return true;
            }
            else if (element.Type == StatElementType.Operator && (Operator)element.Value != Operator.Not && (Operator)element.Value != Operator.LeftParenthesis)
            {
                return true;
            }
            else
            {
                return false;
            }
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
            if (element.Type == StatElementType.Variable || element.Type == StatElementType.Constant)
            {
                return true;
            }
            else if ((Operator)element.Value == Operator.LeftParenthesis)
            {
                return true;
            }
            else if ((Operator)element.Value == Operator.Not)
            {
                return true;
            }
            else
            {
                return false;
            }
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
}