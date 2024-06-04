using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ORMFramework.Ioc;

namespace ORMFramework.Test
{
	[TestClass]
	public class ComponentProviderTest
	{
		[TestMethod]
		public void TestGetSession()
		{
			IComponentProvider componentProvider = new ComponentProviderBuilder()
				.Build();
			ISession session = componentProvider.GetComponent<ISession>();
			Assert.AreEqual<Type>(typeof(Session), session.GetType());
		}
	}
}

