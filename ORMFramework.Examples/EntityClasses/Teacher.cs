namespace ORMFramework.Examples;
public class Teacher
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Gender { get; set; }

    public Department? Department { get; set; }

    public override bool Equals(object? obj) => obj is Teacher other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}