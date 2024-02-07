using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Examples
{
    public class Student
    {
        public string StudentId { get; set; }

        public string Name { get; set; }

        public string Gender { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            Student student = (Student)obj;
            return this.StudentId == student.StudentId;
        }

        public override int GetHashCode()
        {
            return this.StudentId.GetHashCode();
        }
    }
}
