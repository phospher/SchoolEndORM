using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Configuration
{
    public class Configuration
    {
        private IList<EntityMapping> _mappings = new List<EntityMapping>();

        public string ConnectionString { get; set; }

        public string ProviderName { get; set; }

        public IList<EntityMapping> Mappings
        {
            get
            {
                return this._mappings;
            }
        }
    }
}