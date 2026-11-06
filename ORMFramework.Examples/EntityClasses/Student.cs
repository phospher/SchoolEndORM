namespace ORMFramework.Examples;

public class Student
{
    public string? StudentId { get; set; }

    public string? Name { get; set; }

    public string? Gender { get; set; }

    public override bool Equals(object? obj) => obj is Student other && StudentId == other.StudentId;

    public override int GetHashCode() => StudentId!.GetHashCode();
}
