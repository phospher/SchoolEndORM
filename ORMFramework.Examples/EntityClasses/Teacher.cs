namespace ORMFramework.Examples;
public class Teacher
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Gender { get; set; }

    public Department Department { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Teacher student = (Teacher)obj;
        return Id == student.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}