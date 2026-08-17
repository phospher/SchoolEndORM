namespace ORMFramework.Examples;
public class ContactMenu
{
    public int Id { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public Student? Student { get; set; }

    public override bool Equals(object? obj) => obj is ContactMenu other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}