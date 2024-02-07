using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Examples
{
    public class Course
    {
        private int _id;
        private string _name;
        private string _classroom;
        private IList<Student> _students;

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

        public string Classroom
        {
            get { return _classroom; }
            set { _classroom = value; }
        }

        public IList<Student> Students
        {
            get { return _students; }
            set { _students = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            Course student = (Course)obj;
            return this.Id == student.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
