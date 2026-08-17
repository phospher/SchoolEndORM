namespace ORMFramework.Examples;
public class Department
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public IList<Teacher>? Teachers { get; set; }

    public override bool Equals(object? obj) => obj is Department other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}