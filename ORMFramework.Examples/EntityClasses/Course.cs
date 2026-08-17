namespace ORMFramework.Examples;
public class Course
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Classroom { get; set; }

    public IList<Student> Students { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Course student = (Course)obj;
        return Id == student.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}