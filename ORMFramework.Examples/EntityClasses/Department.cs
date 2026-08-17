namespace ORMFramework.Examples;
public class Department
{
    private int _id;
    private string _name;
    private IList<Teacher> _teachers;

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

    public IList<Teacher> Teachers
    {
        get { return _teachers; }
        set { _teachers = value; }
    }

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
