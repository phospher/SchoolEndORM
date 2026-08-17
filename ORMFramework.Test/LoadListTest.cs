using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework;
using ORMFramework.Test.Fakes;

#pragma warning disable CA1707 // test method names are specified with underscores by the task spec

namespace ORMFramework.Test;
[TestClass]
public class LoadListTest
{
    private sealed class Item
    {
        public int Id { get; set; }
    }

    [TestMethod]
    public void GetItems_MaterializesOnce_AndQueriesWithExpectedPredicate()
    {
        var cache = new FakeSessionCache
        {
            SearchResult = new object[] { new Item { Id = 1 }, new Item { Id = 2 } }
        };

        var list = new LoadList<Item>
        {
            SessionCache = cache,
            ForeignKeyName = "fk"
        };
        list.ForeignKeyValues.Add("v1");
        list.ForeignKeyValues.Add("v2");

        int count = list.Count;
        Item first = list[0];
        var viaEnumerator = new List<Item>(list);

        Assert.AreEqual(2, count);
        Assert.AreEqual(1, first.Id);
        Assert.AreEqual(2, viaEnumerator.Count);
        Assert.AreEqual(1, viaEnumerator[0].Id);
        Assert.AreEqual(2, viaEnumerator[1].Id);

        Assert.AreEqual(1, cache.SearchCallCount, "lazy load should materialize exactly once");
        Assert.AreEqual(typeof(Item), cache.SearchTypes[0]);
        Assert.AreEqual("fk=='v1' || fk=='v2'", cache.SearchExpressions[0].Expression);
    }
}
