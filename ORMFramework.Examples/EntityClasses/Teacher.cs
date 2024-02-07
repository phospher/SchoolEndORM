using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Examples
{
    public class Teacher
    {
        private int _id;
        private string _name;
        private string _gender;
        private Department _department;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Gender
        {
            get { return _gender; }
            set { _gender = value; }
        }

        public Department Department
        {
            get { return _department; }
            set { _department = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            Teacher student = (Teacher)obj;
            return this.Id == student.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
