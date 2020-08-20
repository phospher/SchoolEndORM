using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ORMFramework.Statment;

namespace ORMFramework.Cache
{
    public static class ObjectCalculator
    {

        #region IsTargetObject

        public static bool IsTargetObject(object @object, QueryExpression expression)
        {
            List<StatElement> suffixExpression = expression.SuffixExpression;
            Stack<StatElement> resultStack = new Stack<StatElement>();
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
                        throw new Exception("Invalid Statement");
                }
            }
            if (resultStack.Count == 1)
            {
                result = resultStack.Pop();
                if (result.Type == StatElementType.Result || (result.Type == StatElementType.Constant && result.Value is bool))
                {
                    return (bool)result.Value;
                }
                else
                {
                    throw new Exception("Invalid Statement");
                }
            }
            else
            {
                throw new Exception("Invalid Statement");
            }
        }

        private static void ProcessVariable(object @object, StatElement element, Stack<StatElement> resultStack)
        {
            if (@object.GetType().GetProperty((string)element.Value) != null)
            {
                resultStack.Push(element);
            }
            else
            {
                throw new Exception(string.Format("{0} do not contain the property:{1}", @object.GetType(), element.Value));
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

        private static bool IsBooleanOperator(Operator @operator)
        {
            return @operator == Operator.And ||
                @operator == Operator.Not ||
                @operator == Operator.Or;
        }

        private static void ProcessNotOperator(Stack<StatElement> resultStack, object @object)
        {
            if (resultStack.Count == 0)
            {
                throw new Exception("Invalid Statement");
            }

            StatElement element = resultStack.Pop();
            if (element.Type == StatElementType.Result)
            {
                element.Value = !((bool)element.Value);
            }
            else if (element.Type == StatElementType.Variable)
            {
                PropertyInfo property = GetObjectProperty(@object, (string)element.Value);
                if (property.PropertyType == typeof(bool))
                {
                    element = new StatElement();
                    element.Type = StatElementType.Result;
                    element.Value = !((bool)property.GetValue(@object, null));
                }
                else
                {
                    throw new Exception(string.Format("{0}.{1} is not a boolean property", @object.GetType(), property.Name));
                }
            }
            else
            {
                if (element.Value is bool)
                {
                    element = new StatElement();
                    element.Type = StatElementType.Result;
                    element.Value = !((bool)element.Value);
                }
                else
                {
                    throw new Exception(string.Format("{0} is not boolean", element.Value));
                }
            }
            resultStack.Push(element);
        }

        private static void ProcessBooleanOperator(Stack<StatElement> resultStack, Operator @operator, object @object)
        {
            if (resultStack.Count < 2)
            {
                throw new Exception("Invalid Statement");
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
            if (x.Value is bool && y.Value is bool)
            {
                StatElement element = new StatElement();
                element.Type = StatElementType.Result;
                element.Value = CalculateBooleanOperator((bool)x.Value, (bool)y.Value, @operator);
                resultStack.Push(element);
            }
            else
            {
                throw new Exception("Invalid Statement");
            }
        }

        private static void ProcessNotBooleanOperator(Stack<StatElement> resultStack, Operator @operator, object @object)
        {
            if (resultStack.Count < 2)
            {
                throw new Exception("Invalid Statement");
            }

            StatElement y = resultStack.Pop();
            StatElement x = resultStack.Pop();
            StatElement element = new StatElement();
            object xValue;
            object yValue;
            element.Type = StatElementType.Result;
            if (x.Type == StatElementType.Variable)
            {
                xValue = GetObjectProperty(@object, (string)x.Value).GetValue(@object, null);
            }
            else
            {
                xValue = x.Value;
            }
            if (y.Type == StatElementType.Variable)
            {
                yValue = GetObjectProperty(@object, (string)y.Value).GetValue(@object, null);
            }
            else
            {
                yValue = y.Value;
            }
            if (IsString(x, @object) && IsString(y, @object))
            {
                element.Value = CalculateStringOperator(x, y, @operator, @object);
            }
            else if (!(IsString(x, @object) || IsString(y, @object)))
            {
                element.Value = CalculateNotBooleanOperator(Convert.ToDouble(xValue), Convert.ToDouble(yValue), @operator);
            }
            else
            {
                throw new Exception("Invalid Statment");
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
                if (property.PropertyType == typeof(DateTime))
                {
                    xStr = ((DateTime)property.GetValue(@object, null)).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    xStr = (string)property.GetValue(@object, null);
                }
            }
            else
            {
                xStr = x.Value.ToString();
            }
            if (y.Type == StatElementType.Variable)
            {
                PropertyInfo property = GetObjectProperty(@object, (string)y.Value);
                if (property.PropertyType == typeof(DateTime))
                {
                    yStr = ((DateTime)property.GetValue(@object, null)).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    yStr = y.Value.ToString();
                }
            }
            else
            {
                yStr = y.Value.ToString();
            }
            if (xStr[0] == '\'' && xStr[xStr.Length - 1] == '\'')
            {
                xStr = xStr.Substring(1, xStr.Length - 2);
            }
            if (yStr[0] == '\'' && yStr[yStr.Length - 1] == '\'')
            {
                yStr = yStr.Substring(1, yStr.Length - 2);
            }
            switch (@operator)
            {
                case Operator.Equals:
                    return xStr == yStr;
                case Operator.UnEquals:
                    return xStr != yStr;
                case Operator.Like:
                    return CalculateLikeOperator(xStr, yStr, x.Type, y.Type);
                default:
                    throw new Exception("Invalid Statement");
            }
        }

        private static bool CalculateLikeOperator(string x, string y, StatElementType xType, StatElementType yType)
        {
            if (yType == StatElementType.Constant)
            {
                int startIndex = 0;
                int endIndex;
                StringBuilder patten = new StringBuilder();
                Regex pattenRegex = new Regex(@"\[(\S|\s)*]");
                Match pattenMatch = pattenRegex.Match(y);
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
                throw new Exception("Invalid Statement");
            }
        }

        private static bool CalculateBooleanOperator(bool x, bool y, Operator @operator)
        {
            switch (@operator)
            {
                case Operator.And:
                    return x && y;
                case Operator.Or:
                    return x || y;
                default:
                    throw new Exception("Invalid operator");
            }
        }

        private static bool CalculateNotBooleanOperator(double x, double y, Operator @operator)
        {
            switch (@operator)
            {
                case Operator.Equals:
                    return x == y;
                case Operator.Greater:
                    return x > y;
                case Operator.GreaterEquals:
                    return x >= y;
                case Operator.Less:
                    return x < y;
                case Operator.LessEquals:
                    return x <= y;
                case Operator.UnEquals:
                    return x != y;
                default:
                    throw new Exception("Invalid Statement");
            }
        }

        private static PropertyInfo GetObjectProperty(object @object, string propertyName)
        {
            PropertyInfo property = @object.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return property;
            }
            else
            {
                throw new Exception(string.Format("{0} do not have the property:{1}", @object.GetType(), propertyName));
            }
        }

        private static bool IsString(StatElement element, object @object)
        {
            if (element.Type == StatElementType.Variable)
            {
                Type type = @object.GetType().GetProperty((string)element.Value).PropertyType;
                if (type == typeof(string) || type == typeof(DateTime) || type == typeof(char))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (element.Type == StatElementType.Constant)
            {
                string value = (string)element.Value;
                if (value.Length > 1 && value[0] == '\'' && value[value.Length - 1] == '\'')
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new Exception("'element' must a variable or constant");
            }
        }

        #endregion
    }
}