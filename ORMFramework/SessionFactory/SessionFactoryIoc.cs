using System;
using ORMFramework.Configuration;
using System.Collections.Generic;
using ORMFramework.Ioc;

namespace ORMFramework
{
    public class SessionFactoryIoc : ISessionFactory
    {
        private IDbDriverFactory _driverFactory;
        private Dictionary<string, EntityMapping> _mappings;
        private IComponentProvider _componentProvider;

        public Configuration.Configuration Configuration
        {
            get; private set;
        }

        public void Initialize()
        {
            this.Initialize(string.Empty);
        }

        public void Initialize(string configFilePath)
        {
            this.Initialize(configFilePath, new ComponentProviderBuilder().Build());   
        }

        public void Initialize(string configFilePath, IComponentProvider componentProvider)
        {
            this._componentProvider = componentProvider;
            if (_driverFactory != null)
            {
                return;
            }
            _mappings = new Dictionary<string, EntityMapping>();
            if (string.IsNullOrEmpty(configFilePath))
            {
                this.Configuration = new ConfigManager().GetSystemConfiguration();
            }
            else
            {
                this.Configuration = new ConfigManager(configFilePath).GetSystemConfiguration();
            }
            this._driverFactory = new DefaultDbDriverFactory(this.Configuration.ConnectionString,
                this.Configuration.ProviderName);
            foreach (EntityMapping map in this.Configuration.Mappings)
            {
                _mappings.Add(map.ClassName, map);
            }
            LogHelper.LogHelper.InitLog(configFilePath);
        }

        public EntityMapping GetEntityMappingByClassName(string className)
        {
            return _mappings[className];
        }

        public ISession CreateSession()
        {
            ISession session = this._componentProvider.GetComponent<ISession>();
            session.SessionFactory = this;
            session.PersistenceContext.DbDriverFactory.ConnectionString = this.Configuration.ConnectionString;
            session.PersistenceContext.DbDriverFactory.SetProviderName(this.Configuration.ProviderName);
            session.PersistenceContext.Mappings = this._mappings;
            session.PersistenceContext.SQLGenerator.PersistenceContext = session.PersistenceContext;

            return session;
        }
    }
}

