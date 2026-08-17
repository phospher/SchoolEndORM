namespace ORMFramework.Examples;
public class Course
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Classroom { get; set; }

    public IList<Student>? Students { get; set; }

    public override bool Equals(object? obj) => obj is Course other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}