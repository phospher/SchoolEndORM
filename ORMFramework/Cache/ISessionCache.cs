using System;
using System.Collections.Generic;
using ORMFramework.Statment;

namespace ORMFramework.Cache
{
    public interface ISessionCache : IDisposable
    {
        void Commit();
        void Delete(object @object);
        void GetReferenceClasses(object soruceObject);
        void Insert(object @object);
        IPersistenceContext PersistenceContext { get; set; }
        IEnumerable<object> Search(Type objType, QueryExpression expression);
        void Update(object @object);
    }
}