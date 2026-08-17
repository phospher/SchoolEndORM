namespace ORMFramework.Examples;
public class ContactMenu
{
    public int Id { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public Student? Student { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        ContactMenu student = (ContactMenu)obj;
        return Id == student.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}