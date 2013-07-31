// 以下代码由 Microsoft Visual Studio 2005 生成。
// 测试所有者应该检查每个测试的有效性。
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using ORMFramework;
using ORMFramework.SQL;
using ORMFramework.Configuration;
using ORMFramework.Statment;
namespace ORMFramework.Test {
    /// <summary>
    ///这是 ORMFramework.SQLGenerator.SQLGenerator 的测试类，旨在
    ///包含所有 ORMFramework.SQLGenerator.SQLGenerator 单元测试
    ///</summary>
    [TestClass ()]
    public class SQLGeneratorTest {


        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }
        #region 附加测试属性
        // 
        //编写测试时，可使用以下附加属性:
        //
        //使用 ClassInitialize 在运行类中的第一个测试前先运行代码
        //
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //使用 ClassCleanup 在运行完类中的所有测试后再运行代码
        //
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //使用 TestInitialize 在运行每个测试前先运行代码
        //
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //使用 TestCleanup 在运行完每个测试后运行代码
        //
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        private IPersistenceContext pesistenceContext;
        object @object;

    //    private void Init () {
    //        Configuration.ConfigManager config = new ORMFramework.Configuration.ConfigManager ( "E:\\毕业设计\\ORMFramework\\ORMFramework\\Configuration\\Config.xml" );
    //        Dictionary<string, EntityMapping> mappings = new Dictionary<string, EntityMapping> ();
    //        foreach ( EntityMapping mapping in config.GetSystemConfiguration ().Mappings ) {
    //            mappings.Add ( mapping.ClassName, mapping );
    //        }
    //        pesistenceContext = new PersistenceContext ( null, mappings );
    //        @object = new Student ( 1 );
    //        ( ( Student ) @object ).Class = new Class ( 1 );
    //    }

    //    /// <summary>
    //    ///GetDeleteSQL (object) 的测试
    //    ///</summary>
    //    [TestMethod ()]
    //    public void GetDeleteSQLTest () {
    //        Init ();
    //        SQLGenerator target = new SQLGenerator ( pesistenceContext );
    //        string actual = target.GetDeleteSQL ( @object );
    //        Console.WriteLine ( actual );
    //    }

    //    /// <summary>
    //    ///GetInsertSQL (object) 的测试
    //    ///</summary>
    //    [TestMethod ()]
    //    public void GetInsertSQLTest () {
    //        Init ();
    //        SQLGenerator target = new SQLGenerator ( pesistenceContext );
    //        string actual = target.GetInsertSQL ( @object );
    //        Console.WriteLine ( actual );
    //    }

    //    /// <summary>
    //    ///GetSelectSQL (Type) 的测试
    //    ///</summary>
    //    [TestMethod ()]
    //    public void GetSelectSQLTest () {
    //        Init ();
    //        SQLGenerator target = new SQLGenerator ( pesistenceContext );
    //        Type type = typeof ( Student ); // TODO: 初始化为适当的值
    //        string actual = target.GetSelectSQL ( type );
    //        Console.WriteLine ( actual );
    //    }

    //    /// <summary>
    //    ///GetSelectSQL (Type, List&lt;StatElement&gt;) 的测试
    //    ///</summary>
    //    [TestMethod ()]
    //    public void GetSelectSQLTest1 () {
    //        Init ();
    //        SQLGenerator target = new SQLGenerator ( pesistenceContext );
    //        Type type = typeof ( Student ); ; // TODO: 初始化为适当的值
    //        System.Collections.Generic.List<ORMFramework.Statment.StatElement> expression = new List<StatElement> (); // TODO: 初始化为适当的值
    //        StatElement element = new StatElement ();
    //        element.Type = StatElementType.Variable;
    //        element.Value = "Id";
    //        expression.Add ( element );
    //        element = new StatElement ();
    //        element.Type = StatElementType.Operator;
    //        element.Value = Operator.Equals;
    //        expression.Add ( element );
    //        element = new StatElement ();
    //        element.Type = StatElementType.Constant;
    //        element.Value = 1;
    //        expression.Add ( element );

    //        string actual = target.GetSelectSQL ( type, expression );

    //        Console.WriteLine ( actual );
    //    }

    //    /// <summary>
    //    ///GetUpdateSQL (object, object) 的测试
    //    ///</summary>
    //    [TestMethod ()]
    //    public void GetUpdateSQLTest () {
    //        Init ();
    //        SQLGenerator target = new SQLGenerator ( pesistenceContext );

    //        object oldObject = new Student ( 1 ); // TODO: 初始化为适当的值
    //        ( ( Student ) oldObject ).Class = new Class ( 2 );
    //        object newObject = new Student ( 2 );
    //        ( ( Student ) newObject ).Class = new Class ( 1 );// TODO: 初始化为适当的值
    //        string actual;
    //        actual = target.GetUpdateSQL ( oldObject, newObject );

    //        Console.WriteLine ( actual );
    //    }


    }
    public class Student {
        private int _id;
        private Class _class;
        private string _name;

        public int Id {
            get { return _id; }
            set { _id = value; }
        }

        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        public Student ( int id ) {
            _id = id;
        }

        public Class Class {
            get { return _class; }
            set { _class = value; }
        }
    }
    public class Class {
        private int _id;

        public Class ( int id ) {
            _id = id;
        }

        public int Id {
            get { return _id; }
            set { _id = value; }
        }
    }

}
