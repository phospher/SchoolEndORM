using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Statment
{
    public enum Operator
    {
        Equals = 0,
        UnEquals = 1,
        Greater = 2,
        GreaterEquals = 3,
        Less = 4,
        LessEquals = 5,
        And = 6,
        Or = 7,
        Not = 8,
        Like = 9,
        LeftParenthesis = 10,
        RightParenthesis = 11
    }
}