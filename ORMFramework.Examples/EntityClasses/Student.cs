using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Examples {
    public class Student {
        private string _studentId;
        private string _name;
        private string _gender;

        public string StudentId {
            get { return _studentId; }
            set { _studentId = value; }
        }

        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        public string Gender {
            get { return _gender; }
            set { _gender = value; }
        }
    }
}
