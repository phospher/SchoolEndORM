using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Statment {
    public class StatElement {
        private object _value;
        private StatElementType _type;

        public object Value {
            get { return _value; }
            set { _value = value; }
        }

        public StatElementType Type {
            get { return _type; }
            set { _type = value; }
        }
    }

    public enum StatElementType {
        Variable,
        Constant,
        Operator,
        Result
    }
}
