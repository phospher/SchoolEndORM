using System.Collections.Frozen;
using System.Data;
using System.Reflection;
using System.Text;
using ORMFramework.Configuration;
using ORMFramework.Exceptions;
using ORMFramework.SQL;
using ORMFramework.Statment;

namespace ORMFramework.Cache;
public static class GlobalCache
{
    private static int _cacheMaxCount = 1000;
    private const int CacheIncrease = 100;
    private static readonly Dictionary<Type, List<GlobalCacheEntity>> _objTable = new();
    private static readonly Dictionary<Guid, GlobalCacheEntity> _objList = new();
    private static readonly FrozenSet<Type> _basicType = FrozenSet.ToFrozenSet(
    [
        typeof(bool), typeof(byte), typeof(sbyte),
        typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint), typeof(long),
        typeof(ulong), typeof(short), typeof(ushort), typeof(char), typeof(string), typeof(byte[])
    ]);

    public static IEnumerable<GlobalCacheResult> Search<T>(QueryExpression? queryExpression, IPersistenceContext persistenceContext, IList<Guid> ignoreIds)
    {
        return Search(typeof(T), queryExpression, persistenceContext, ignoreIds);
    }

    public static IEnumerable<GlobalCacheResult> Search(Type objType, QueryExpression? queryExpression, IPersistenceContext persistenceContext, IList<Guid> ignoreIds)
    {
        var result = new List<GlobalCacheResult>();
        EntityMapping mapping = persistenceContext.GetEntityMappingByClassName(objType.FullName ?? throw new OrmConfigurationException($"Type '{objType}' has no full name."))
            ?? throw new OrmConfigurationException($"No mapping is registered for type '{objType}'.");
        var ignorePrimaryKey = new Dictionary<string, List<object>>();
        StringBuilder sql;
        ISQLGenerator sqlGenerator = persistenceContext.SQLGenerator;
        if (queryExpression == null)
        {
            sql = new StringBuilder(sqlGenerator.GetSelectSQL(objType));
        }
        else
        {
            sql = new StringBuilder(sqlGenerator.GetSelectSQL(objType, queryExpression));
        }
        if (_objTable.TryGetValue(objType, out var entities))
        {
            foreach (GlobalCacheEntity entity in entities)
            {
                if (ignoreIds != null && ignoreIds.Contains(entity.ObjectId))
                {
                    foreach (string key in mapping.Keys)
                    {
                        if (!ignorePrimaryKey.TryGetValue(key, out var values))
                        {
                            values = [];
                            ignorePrimaryKey[key] = values;
                        }
                        values.Add((objType.GetProperty(key) ?? throw new OrmConfigurationException($"Mapping references unknown property '{key}' on {objType}.")).GetValue(entity.Value)!);
                    }
                }
                else
                {
                    try
                    {
                        ReadObject(entity, queryExpression, result, ignorePrimaryKey, mapping);
                    }
                    catch (ThreadInterruptedException)
                    {
                        ReadObject(entity, queryExpression, result, ignorePrimaryKey, mapping);
                    }
                }
            }
        }
        ReadIgnoreKeys(ignorePrimaryKey, sql, queryExpression != null);
        ReadDataFromDatabase(objType, sql.ToString(), result, persistenceContext);
        return result;
    }

    public static GlobalCacheResult? Search(Guid objectId)
    {
        if (_objList.TryGetValue(objectId, out var cacheEntity))
        {
            if (!ProcessDeletedObject(cacheEntity))
            {
                if (cacheEntity.Lock == Lock.WriteLock && cacheEntity.WriteLockThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    try
                    {
                        cacheEntity.BlockThreads.Enqueue(Thread.CurrentThread);
                        Monitor.Pulse(cacheEntity);
                        Monitor.Exit(cacheEntity);
                        Thread.Sleep(Timeout.Infinite);
                        return null;
                    }
                    catch (ThreadInterruptedException)
                    {
                        return Search(objectId);
                    }
                }
                else if (cacheEntity.Lock == Lock.WriteLock)
                {
                    Monitor.PulseAll(cacheEntity);
                    Monitor.Exit(cacheEntity);
                    var result = new GlobalCacheResult(cacheEntity.ObjectId, CloneObject(cacheEntity.Value), cacheEntity.Version);
                    result.ForeignKeys = cacheEntity.ForeignKeys;
                    return result;
                }
                else
                {
                    cacheEntity.Lock = Lock.ReadLock;
                    cacheEntity.ReadCount++;
                    Monitor.Pulse(cacheEntity);
                    Monitor.Exit(cacheEntity);
                    var result = new GlobalCacheResult(cacheEntity.ObjectId, CloneObject(cacheEntity.Value), cacheEntity.Version);
                    result.ForeignKeys = cacheEntity.ForeignKeys;
                    lock (cacheEntity)
                    {
                        cacheEntity.ReadCount--;
                        if (cacheEntity.ReadCount == 0)
                        {
                            cacheEntity.Lock = Lock.None;
                            if (cacheEntity.BlockThreads.Count > 0)
                            {
                                cacheEntity.BlockThreads.Dequeue().Interrupt();
                            }
                        }
                        Monitor.Pulse(cacheEntity);
                    }
                    return result;
                }
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public static void Update(Guid objectId, object newObj, Dictionary<string, object> foreignKeys, IPersistenceContext persistenceContext)
    {
        object @object = CloneObject(newObj);
        EntityMapping? mapping = persistenceContext.GetEntityMappingByClassName(newObj.GetType().FullName ?? throw new OrmConfigurationException($"Type '{newObj.GetType()}' has no full name."));
        if (!_objList.TryGetValue(objectId, out var cacheEntity))
        {
            throw new ConcurrencyException("object is not in the second-level cache");
        }
        if (cacheEntity.Lock != Lock.WriteLock || cacheEntity.WriteLockThreadId != Thread.CurrentThread.ManagedThreadId)
        {
            throw new ConcurrencyException("You must get a WriteLock before updating object");
        }
        cacheEntity.Value = @object;
        cacheEntity.Version = DateTime.UtcNow.Ticks;
        cacheEntity.ForeignKeys = foreignKeys;
        cacheEntity.WriteLockThreadId = -1;
        cacheEntity.Lock = Lock.None;
        if (cacheEntity.BlockThreads.Count > 0)
        {
            cacheEntity.BlockThreads.Dequeue().Interrupt();
        }
    }

    public static void GetWriteLockForObject(Guid objectId)
    {
        GlobalCacheEntity cacheEntity = _objList[objectId];
        if (!ProcessDeletedObject(cacheEntity))
        {
            if (cacheEntity.Lock == Lock.None)
            {
                cacheEntity.Lock = Lock.WriteLock;
                cacheEntity.WriteLockThreadId = Thread.CurrentThread.ManagedThreadId;
                Monitor.Pulse(cacheEntity);
                Monitor.Exit(cacheEntity);
            }
            else if (cacheEntity.Lock == Lock.WriteLock && cacheEntity.WriteLockThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                Monitor.Pulse(cacheEntity);
                Monitor.Exit(cacheEntity);
            }
            else
            {
                try
                {
                    cacheEntity.BlockThreads.Enqueue(Thread.CurrentThread);
                    Monitor.Pulse(cacheEntity);
                    Monitor.Exit(cacheEntity);
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {
                    GetWriteLockForObject(objectId);
                }
            }
        }
        else
        {
            throw new ConcurrencyException("object has been deleted");
        }
    }

    public static long GetObjectVersion(Guid objectId) => _objList.TryGetValue(objectId, out var e) ? e.Version : -1;

    public static void Delete(Guid objectId)
    {
        GlobalCacheEntity entity = _objList[objectId];
        if (entity.Lock != Lock.WriteLock || entity.WriteLockThreadId != Thread.CurrentThread.ManagedThreadId)
        {
            throw new ConcurrencyException("You must get a WriteLock before deleting object");
        }
        entity.IsDeleted = true;
        if (entity.BlockThreads.Count > 0)
        {
            entity.BlockThreads.Dequeue().Interrupt();
        }
        else
        {
            DeleteCacheEntity(entity);
        }
    }

    public static void Insert(CacheEntity entity)
    {
        var cacheEntity = new GlobalCacheEntity();
        cacheEntity.ObjectId = entity.ObjectId;
        cacheEntity.Value = entity.Value;
        cacheEntity.Version = entity.Version;
        cacheEntity.ForeignKeys = entity.ForeignKeys;
        cacheEntity.ReferenceCount++;
        CheckCache();
        _objList.Add(cacheEntity.ObjectId, cacheEntity);
        if (!_objTable.TryGetValue(cacheEntity.Value.GetType(), out var list))
        {
            list = [];
            _objTable[cacheEntity.Value.GetType()] = list;
        }
        list.Add(cacheEntity);
    }

    public static void ReleaseWriteLock(Guid objectId)
    {
        GlobalCacheEntity cacheEntity = _objList[objectId];
        lock (cacheEntity)
        {
            if (cacheEntity.Lock == Lock.WriteLock && cacheEntity.WriteLockThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                cacheEntity.Lock = Lock.None;
                if (cacheEntity.BlockThreads.Count > 0)
                {
                    cacheEntity.BlockThreads.Dequeue().Interrupt();
                }
                Monitor.PulseAll(cacheEntity);
            }
        }
    }

    private static void DeleteCacheEntity(GlobalCacheEntity cacheEntity)
    {
        if (cacheEntity.IsDeleted)
        {
            Type objType = cacheEntity.Value.GetType();
            _objList.Remove(cacheEntity.ObjectId);
            _objTable[objType].Remove(cacheEntity);
            if (_objTable[objType].Count == 0)
            {
                _objTable.Remove(objType);
            }
        }
    }

    private static bool ProcessDeletedObject(GlobalCacheEntity cacheEntity)
    {
        Monitor.Enter(cacheEntity);
        if (cacheEntity.IsDeleted)
        {
            if (cacheEntity.BlockThreads.Count > 0)
            {
                Thread[] threads = cacheEntity.BlockThreads.ToArray();
                cacheEntity.BlockThreads.Clear();
                Monitor.Pulse(cacheEntity);
                Monitor.Exit(cacheEntity);
                foreach (Thread thread in threads)
                {
                    thread.Interrupt();
                }
            }
            else
            {
                Monitor.Pulse(cacheEntity);
                Monitor.Exit(cacheEntity);
                DeleteCacheEntity(cacheEntity);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private static void ReadObject(GlobalCacheEntity entity, QueryExpression? queryExpression, List<GlobalCacheResult> resultList, Dictionary<string, List<object>> ignoreKeys, EntityMapping mapping)
    {
        if (!ProcessDeletedObject(entity))
        {
            if (queryExpression == null || ObjectCalculator.IsTargetObject(entity.Value, queryExpression))
            {
                if (entity.Lock == Lock.WriteLock && entity.WriteLockThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    entity.BlockThreads.Enqueue(Thread.CurrentThread);
                    Monitor.Pulse(entity);
                    Monitor.Exit(entity);
                    Thread.Sleep(Timeout.Infinite);
                }
                else
                {
                    entity.Lock = Lock.ReadLock;
                    entity.ReadCount++;
                    Monitor.PulseAll(entity);
                    Monitor.Exit(entity);
                    var cacheResult = new GlobalCacheResult(entity.ObjectId, CloneObject(entity.Value), entity.Version);
                    cacheResult.ForeignKeys = entity.ForeignKeys;
                    resultList.Add(cacheResult);
                    foreach (string key in mapping.Keys)
                    {
                        if (!ignoreKeys.TryGetValue(key, out var values))
                        {
                            values = [];
                            ignoreKeys[key] = values;
                        }
                        values.Add((entity.Value.GetType().GetProperty(key) ?? throw new OrmConfigurationException($"Mapping references unknown property '{key}' on {entity.Value.GetType()}.")).GetValue(entity.Value)!);
                    }
                    lock (entity)
                    {
                        entity.ReadCount--;
                        entity.ReferenceCount++;
                        if (entity.ReadCount == 0)
                        {
                            entity.Lock = Lock.None;
                            if (entity.BlockThreads.Count > 0)
                            {
                                entity.BlockThreads.Dequeue().Interrupt();
                            }
                        }
                        Monitor.PulseAll(entity);
                    }
                }
            }
            else
            {
                Monitor.PulseAll(entity);
                Monitor.Exit(entity);
            }
        }
    }

    private static object CloneObject(object @object)
    {
        Type objType = @object.GetType();
        object newobject = Activator.CreateInstance(objType) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {objType}");
        foreach (PropertyInfo property in objType.GetProperties())
        {
            if (IsBasicType(property.PropertyType))
            {
                object? value = property.GetValue(@object);
                property.SetValue(newobject, value);
            }
        }
        return newobject;
    }

    private static bool IsBasicType(Type type) => _basicType.Contains(type);

    private static void ReadIgnoreKeys(Dictionary<string, List<object>> ignoreKeys, StringBuilder sql, bool hasExpression)
    {
        int i = 0;
        foreach (var (key, values) in ignoreKeys)
        {
            int j = 0;
            if (i == 0 && !hasExpression)
            {
                sql.Append(" where ");
            }
            else
            {
                sql.Append(" and ");
            }
            sql.Append(key);
            sql.Append(" not in (");
            foreach (object o in values)
            {
                sql.Append(SqlLiteral.Format(o));
                j++;
                if (j < values.Count)
                {
                    sql.Append(",");
                }
            }
            sql.Append(")");
        }
    }

    private static void ReadDataFromDatabase(Type objType, string sql, List<GlobalCacheResult> resultList, IPersistenceContext persistenceContext)
    {
        var conn = persistenceContext.Connection;
        using var ds = new DataSet();
        IDbDataAdapter dataAdapter = persistenceContext.DbDriverFactory.GetDbDataAdapter(sql, conn);
        using (dataAdapter as IDisposable)
        {
            dataAdapter.Fill(ds);
        }
        DataTable dt = ds.Tables[0];
        if (dt.Rows.Count > 0)
        {
            if (!_objTable.TryGetValue(objType, out var objectList))
            {
                objectList = [];
                _objTable[objType] = objectList;
            }
            lock (objectList)
            {
                foreach (DataRow row in dt.Rows)
                {
                    object newObj = Activator.CreateInstance(objType) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {objType}");
                    EntityMapping mapping = persistenceContext.GetEntityMappingByClassName(objType.FullName ?? throw new OrmConfigurationException($"Type '{objType}' has no full name."))
                        ?? throw new OrmConfigurationException($"No mapping is registered for type '{objType}'.");
                    foreach (PropertyInfo property in objType.GetProperties())
                    {
                        if (IsBasicType(property.PropertyType))
                        {
                            property.SetValue(newObj, row[property.Name]);
                        }
                    }
                    var cacheEntity = new GlobalCacheEntity(newObj);
                    foreach (EntityRelation relation in mapping.Relations.Values)
                    {
                        cacheEntity.ForeignKeys.Add(relation.KeyColum, row[relation.KeyColum]);
                    }
                    CheckCache();
                    _objList.Add(cacheEntity.ObjectId, cacheEntity);
                    if (!_objTable.TryGetValue(objType, out var currentList))
                    {
                        currentList = [];
                        _objTable[objType] = currentList;
                    }
                    currentList.Add(cacheEntity);
                    var cacheResult = new GlobalCacheResult(cacheEntity.ObjectId, CloneObject(newObj), cacheEntity.Version);
                    cacheResult.ForeignKeys = cacheEntity.ForeignKeys;
                    cacheEntity.ReferenceCount++;
                    resultList.Add(cacheResult);
                }
                Monitor.PulseAll(_objTable[objType]);
            }
        }
    }

    public static void Detach(Guid objectId)
    {
        GlobalCacheEntity cacheEntity = _objList[objectId];
        if (cacheEntity.ReferenceCount == 0)
        {
            throw new InvalidOperationException("The object do not be referenced");
        }
        _objList[objectId].ReferenceCount--;
    }

    public static void ReleaseReferenct(Guid objectid)
    {
        GlobalCacheEntity cacheEntity = _objList[objectid];
        lock (cacheEntity)
        {
            if (cacheEntity.ReferenceCount <= 0)
            {
                return;
            }
            cacheEntity.ReferenceCount--;
        }
    }

    private static void Collect()
    {
        GC.Collect();
        var keysToDelete = new List<Guid>();
        foreach (var (objectId, cacheEntity) in _objList)
        {
            if (cacheEntity.ReferenceCount <= 0)
            {
                keysToDelete.Add(objectId);
            }
        }
        foreach (Guid objectId in keysToDelete)
        {
            GlobalCacheEntity cacheEntity = _objList[objectId];
            List<GlobalCacheEntity> objectList = _objTable[cacheEntity.Value.GetType()];
            _objList.Remove(objectId);
            objectList.Remove(cacheEntity);
            if (objectList.Count == 0)
            {
                _objTable.Remove(cacheEntity.Value.GetType());
            }
        }
        GC.Collect();
        if (_objList.Count >= _cacheMaxCount)
        {
            _cacheMaxCount += CacheIncrease;
        }
    }

    private static void CheckCache()
    {
        if (_objList.Count >= _cacheMaxCount)
        {
            Collect();
        }
    }
}