using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Exceptions;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;

[TestClass]
public class OrmExceptionTest
{
    [TestMethod]
    public void OrmException_CatchesAllThreeSpecificFailureKinds()
    {
        Assert.IsTrue(IsOrmException(() => throw new QuerySyntaxException("syntax")));
        Assert.IsTrue(IsOrmException(() => throw new OrmConfigurationException("config")));
        Assert.IsTrue(IsOrmException(() => throw new ConcurrencyException("concurrency")));
    }

    [TestMethod]
    public void OrmException_IsStillCatchableAsGenericException()
    {
        // 行为不变：OrmException 仍是 Exception 的子类，按 Exception 捕获的调用方不受影响。
        Assert.IsInstanceOfType(new QuerySyntaxException("x"), typeof(Exception));
        Assert.IsTrue(IsException(() => throw new OrmException("orm")));
    }

    private static bool IsOrmException(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (OrmException)
        {
            return true;
        }
    }

    private static bool IsException(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}
