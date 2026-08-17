namespace ORMFramework.Examples;
public class Department
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public IList<Teacher>? Teachers { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Department student = (Department)obj;
        return Id == student.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}