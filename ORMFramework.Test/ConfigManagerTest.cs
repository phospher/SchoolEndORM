using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Configuration;
using SystemConfiguration = ORMFramework.Configuration.Configuration;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;

[TestClass]
public sealed class ConfigManagerTest
{
    private static SystemConfiguration GetConfiguration()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "Config.xml");
        return new ConfigManager(path).GetSystemConfiguration();
    }

    [TestMethod]
    public void GetSystemConfiguration_ParsesFiveMappings()
    {
        SystemConfiguration config = GetConfiguration();
        Assert.AreEqual(5, config.Mappings.Count);
    }

    [TestMethod]
    [DataRow("ORMFramework.Examples.Student", "Student")]
    [DataRow("ORMFramework.Examples.Department", "Department")]
    [DataRow("ORMFramework.Examples.Teacher", "Teacher")]
    [DataRow("ORMFramework.Examples.ContactMenu", "ContactMenu")]
    [DataRow("ORMFramework.Examples.Course", "Course")]
    public void GetSystemConfiguration_Mappings_HaveExpectedClassAndTableNames(string className, string tableName)
    {
        Dictionary<string, EntityMapping> mappings = GetConfiguration().Mappings.ToDictionary(m => m.ClassName);

        Assert.AreEqual(tableName, mappings[className].TableName);
    }

    [TestMethod]
    public void GetSystemConfiguration_Mappings_HaveExpectedKeys()
    {
        Dictionary<string, EntityMapping> mappings = GetConfiguration().Mappings.ToDictionary(m => m.ClassName);

        Assert.AreEqual("StudentId", mappings["ORMFramework.Examples.Student"].Keys.Single());
        Assert.AreEqual("Id", mappings["ORMFramework.Examples.Department"].Keys.Single());
        Assert.AreEqual("Id", mappings["ORMFramework.Examples.Teacher"].Keys.Single());
        Assert.AreEqual("StudentId", mappings["ORMFramework.Examples.ContactMenu"].Keys.Single());
        Assert.AreEqual("Id", mappings["ORMFramework.Examples.Course"].Keys.Single());
    }

    [TestMethod]
    public void GetSystemConfiguration_ContainsAllFourRelationTypes()
    {
        List<RelationType> types = GetConfiguration().Mappings
            .SelectMany(m => m.Relations.Values)
            .Select(r => r.Type)
            .ToList();

        Assert.Contains(RelationType.OneToOne, types);
        Assert.Contains(RelationType.OneToMany, types);
        Assert.Contains(RelationType.ManyToOne, types);
        Assert.Contains(RelationType.MantToMany, types);
    }

    [TestMethod]
    public void GetSystemConfiguration_Relations_HaveExpectedColumns()
    {
        Dictionary<string, EntityMapping> mappings = GetConfiguration().Mappings.ToDictionary(m => m.ClassName);

        EntityRelation oneToMany = mappings["ORMFramework.Examples.Department"].Relations["Teachers"];
        Assert.AreEqual(RelationType.OneToMany, oneToMany.Type);
        Assert.AreEqual("Id", oneToMany.KeyColum);
        Assert.AreEqual("DepartmentId", oneToMany.ReferenceColum);
        Assert.AreEqual("ORMFramework.Examples.Teacher", oneToMany.ReferenceClassName);

        EntityRelation manyToOne = mappings["ORMFramework.Examples.Teacher"].Relations["Department"];
        Assert.AreEqual(RelationType.ManyToOne, manyToOne.Type);
        Assert.AreEqual("DepartmentId", manyToOne.KeyColum);
        Assert.AreEqual("Id", manyToOne.ReferenceColum);
        Assert.AreEqual("Department", manyToOne.ReferenceClassName);

        EntityRelation oneToOne = mappings["ORMFramework.Examples.ContactMenu"].Relations["Student"];
        Assert.AreEqual(RelationType.OneToOne, oneToOne.Type);
        Assert.AreEqual("StudentId", oneToOne.KeyColum);
        Assert.AreEqual("StudentId", oneToOne.ReferenceColum);
        Assert.AreEqual("ORMFramework.Examples.Student", oneToOne.ReferenceClassName);

        var manyToMany = (ManyToManyRelation)mappings["ORMFramework.Examples.Course"].Relations["Students"];
        Assert.AreEqual(RelationType.MantToMany, manyToMany.Type);
        Assert.AreEqual("Id", manyToMany.KeyColum);
        Assert.AreEqual("Course_Student", manyToMany.ReferenceTableName);
        Assert.AreEqual("CourseId", manyToMany.ReferenceColum);
        Assert.AreEqual("StudentId", manyToMany.ReferenceClassColum);
        Assert.AreEqual("StudentId", manyToMany.ReferenceClassKeyColum);
        Assert.AreEqual("Student", manyToMany.ReferenceClassName);
    }
}
