using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Cache;
using ORMFramework.Configuration;
using ORMFramework.SQL;

namespace ORMFramework
{
    public class SessionFactory : ISessionFactory
    {
        private IDbDriverFactory _driverFactory;
        private Dictionary<string, EntityMapping> _mappings;

        public Configuration.Configuration Configuration
        {
            get; private set;
        }

        public void Initialize()
        {
            Initialize(string.Empty);
        }

        public void Initialize(string configFilePath)
        {
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
            IPersistenceContext persistenceContext = new PersistenceContext(_driverFactory, _mappings);
            persistenceContext.SQLGenerator = new SQLGenerator();
            persistenceContext.SQLGenerator.PersistenceContext = persistenceContext;
            return new Session(this, persistenceContext);
        }
    }
}