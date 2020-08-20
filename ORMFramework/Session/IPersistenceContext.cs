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
        EntityMapping GetEntityMappingByClassName(string className);
    }
}