using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ORMFramework.Configuration;

namespace ORMFramework
{
    public interface IPersistenceContext
    {
        IDbDriverFactory DbDriverFactory { get; }
        IDbConnection Connection { get; }
        IDictionary<string, EntityMapping> Mappings { get; set; }
        EntityMapping GetEntityMappingByClassName(string className);

    }
}