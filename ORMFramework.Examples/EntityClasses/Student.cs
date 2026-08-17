namespace ORMFramework.Examples;
public class Student
{
    public string? StudentId { get; set; }

    public string? Name { get; set; }

    public string? Gender { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Student student = (Student)obj;
        return StudentId == student.StudentId;
    }

    public override int GetHashCode()
    {
        return StudentId!.GetHashCode();
    }
}
