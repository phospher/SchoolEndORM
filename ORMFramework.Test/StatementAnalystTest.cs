// 以下代码由 Microsoft Visual Studio 2005 生成。
// 测试所有者应该检查每个测试的有效性。
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using ORMFramework.Statment;
using System.Collections;
using ORMFramework;
using System.Reflection;
namespace ORMFramework.Test
{
    /// <summary>
    ///这是 ORMFramework.Statment.StatementAnalyst 的测试类，旨在
    ///包含所有 ORMFramework.Statment.StatementAnalyst 单元测试
    ///</summary>
    [TestClass()]
    public class StatementAnalystTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
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


        /// <summary>
        ///Analysis (string) 的测试
        ///</summary>
        //[TestMethod ()]
        //public void AnalysisTest () {
        //    StatementAnalyst target = new StatementAnalyst ();

        //    string queryString = "Student like '%' && a=='2' || ! ( b== 3 )"; // TODO: 初始化为适当的值

        //    System.Collections.Generic.List<ORMFramework.Statment.StatElement> expected = null;
        //    System.Collections.Generic.List<ORMFramework.Statment.StatElement> actual;

        //    target.QueryString = queryString;
        //    actual = target.GetSuffixExpression ();
        //    foreach ( StatElement element in actual ) {
        //        if ( element.Type == StatElementType.Variable || element.Type == StatElementType.Constant ) {
        //            Console.Write ( "{0} ", element.Value );
        //        } else {
        //            switch ( ( Operator ) element.Value ) {
        //                case Operator.And: Console.Write ( "{0} ", "&&" );
        //                    break;
        //                case Operator.Equals: Console.Write ( "{0} ", "==" );
        //                    break;
        //                case Operator.Greater: Console.Write ( "{0} ", ">" );
        //                    break;
        //                case Operator.GreaterEquals: Console.Write ( "{0} ", ">=" );
        //                    break;
        //                case Operator.LeftParenthesis: Console.Write ( "{0} ", "(" );
        //                    break;
        //                case Operator.Less: Console.Write ( "{0} ", "<" );
        //                    break;
        //                case Operator.LessEquals: Console.Write ( "{0} ", "<=" );
        //                    break;
        //                case Operator.Like: Console.Write ( "{0} ", "like" );
        //                    break;
        //                case Operator.Not: Console.Write ( "{0} ", "!" );
        //                    break;
        //                case Operator.Or: Console.Write ( "{0} ", "||" );
        //                    break;
        //                case Operator.RightParenthesis: Console.Write ( "{0} ", ")" );
        //                    break;
        //                case Operator.UnEquals: Console.Write ( "{0} ", "!=" );
        //                    break;
        //            }
        //        }
        //    }
        //    return;
        //}

        //[TestMethod ()]
        //public void test () {
        //    Dictionary<int, char> d = new Dictionary<int, char> ();
        //    d.Add ( 1, 'a' );
        //    d.Add ( 2, 'b' );
        //    d.Add ( 3, 'c' );
        //    d.Add ( 4, 'd' );
        //    for ( int i = d.Values.Count - 1; i >= 0; i-- ) {
        //        if ( d.Values[i] == 'b' ) {
        //            d.Remove ( 2 );
        //        }
        //    }
        //    foreach ( KeyValuePair<int, char> item in d ) {
        //        Console.WriteLine ( "{0}--{1}", item.Key, item.Value );
        //    }
        //}

    }

    public class List2 : ICollection<int>
    {
        #region ICollection<int> 成员

        public void Add(int item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Clear()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Contains(int item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool IsReadOnly
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool Remove(int item)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerable<int> 成员

        public IEnumerator<int> GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerable 成员

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
