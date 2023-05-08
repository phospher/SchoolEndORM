using System;
namespace ORMFramework
{
	public interface ISessionFactory
	{
        void Initialize();
        void Initialize(string configFilePath);
        ISession CreateSession();
    }
}

