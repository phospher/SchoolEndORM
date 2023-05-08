using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using ORMFramework.Configuration;
using ORMFramework.SQL;
using ORMFramework.Statment;

namespace ORMFramework.Cache
{
    public static class GlobalCache
    {
        private static int CACHEMAXCOUNT = 1000;
        private static int INCREASE = 100;
        private static Dictionary<Type, List<GlobalCacheEntity>> _objTable = new Dictionary<Type, List<GlobalCacheEntity>>();
        private static Dictionary<Guid, GlobalCacheEntity> _objList = new Dictionary<Guid, GlobalCacheEntity>();
        private static readonly Type[] _basicType = new Type[] {
            typeof (bool), typeof (byte), typeof (sbyte),
            typeof (decimal), typeof (double), typeof (float), typeof (int), typeof (uint), typeof (long),
            typeof (ulong), typeof (short), typeof (ushort), typeof (char), typeof (string), typeof (byte[])
        };

        public static IEnumerable<GlobalCacheResult> Search<T>(QueryExpression queryExpression, IPersistenceContext persistenceContext, IList<Guid> ignoreIds)
        {
            return Search(typeof(T), queryExpression, persistenceContext, ignoreIds);
        }

        public static IEnumerable<GlobalCacheResult> Search(Type objType, QueryExpression queryExpression, IPersistenceContext persistenceContext, IList<Guid> ignoreIds)
        {
            List<GlobalCacheResult> result = new List<GlobalCacheResult>();
            EntityMapping mapping = persistenceContext.GetEntityMappingByClassName(objType.FullName);
            Dictionary<string, List<object>> ignorePrimaryKey = new Dictionary<string, List<object>>();
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
            if (_objTable.ContainsKey(objType))
            {
                foreach (GlobalCacheEntity entity in _objTable[objType])
                {
                    if (ignoreIds != null && ignoreIds.Contains(entity.ObjectId))
                    {
                        foreach (string key in mapping.Keys)
                        {
                            if (!ignorePrimaryKey.ContainsKey(key))
                            {
                                ignorePrimaryKey.Add(key, new List<object>());
                            }
                            ignorePrimaryKey[key].Add(objType.GetProperty(key).GetValue(entity.Value, null));
                        }
                    }
                    else
                    {
                        try
                        {
                            ReadObject(entity, queryExpression, result, ignorePrimaryKey, mapping);
                        }
                        catch (ThreadInterruptedException ex)
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

        public static GlobalCacheResult Search(Guid objectId)
        {
            if (_objList.ContainsKey(objectId))
            {
                GlobalCacheEntity cacheEntity = _objList[objectId];
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
                        catch (ThreadInterruptedException ex)
                        {
                            return Search(objectId);
                        }
                    }
                    else if (cacheEntity.Lock == Lock.WriteLock)
                    {
                        Monitor.PulseAll(cacheEntity);
                        Monitor.Exit(cacheEntity);
                        GlobalCacheResult result = new GlobalCacheResult(cacheEntity.ObjectId, CloneObject(cacheEntity.Value), cacheEntity.Version);
                        result.ForeignKeys = cacheEntity.ForeignKeys;
                        return result;
                    }
                    else
                    {
                        cacheEntity.Lock = Lock.ReadLock;
                        cacheEntity.ReadCount++;
                        Monitor.Pulse(cacheEntity);
                        Monitor.Exit(cacheEntity);
                        GlobalCacheResult result = new GlobalCacheResult(cacheEntity.ObjectId, CloneObject(cacheEntity.Value), cacheEntity.Version);
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
            EntityMapping mapping = persistenceContext.GetEntityMappingByClassName(newObj.GetType().FullName);
            GlobalCacheEntity cacheEntity = _objList[objectId];
            if (cacheEntity.Lock != Lock.WriteLock || cacheEntity.WriteLockThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new Exception("You must get a WriteLock before updating object");
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
                    catch (ThreadInterruptedException ex)
                    {
                        GetWriteLockForObject(objectId);
                    }
                }
            }
            else
            {
                throw new Exception("object has been deleted");
            }
        }

        public static long GetObjectVersion(Guid objectId)
        {
            if (_objList.ContainsKey(objectId))
            {
                return _objList[objectId].Version;
            }
            else
            {
                return -1;
            }
        }

        public static void Delete(Guid objectId)
        {
            GlobalCacheEntity entity = _objList[objectId];
            if (entity.Lock != Lock.WriteLock || entity.WriteLockThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                throw new Exception("You must get a WriteLock before deleting object");
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
            GlobalCacheEntity cacheEntity = new GlobalCacheEntity();
            cacheEntity.ObjectId = entity.ObjectId;
            cacheEntity.Value = entity.Value;
            cacheEntity.Version = entity.Version;
            cacheEntity.ForeignKeys = entity.ForeignKeys;
            cacheEntity.ReferenceCount++;
            CheckCache();
            _objList.Add(cacheEntity.ObjectId, cacheEntity);
            if (!_objTable.ContainsKey(cacheEntity.Value.GetType()))
            {
                _objTable.Add(cacheEntity.Value.GetType(), new List<GlobalCacheEntity>());
            }
            _objTable[cacheEntity.Value.GetType()].Add(cacheEntity);
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

        private static void ReadObject(GlobalCacheEntity entity, QueryExpression queryExpression, List<GlobalCacheResult> resultList, Dictionary<string, List<object>> ignoreKeys, EntityMapping mapping)
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
                        GlobalCacheResult cacheResult = new GlobalCacheResult(entity.ObjectId, CloneObject(entity.Value), entity.Version);
                        cacheResult.ForeignKeys = entity.ForeignKeys;
                        resultList.Add(cacheResult);
                        foreach (string key in mapping.Keys)
                        {
                            if (!ignoreKeys.ContainsKey(key))
                            {
                                ignoreKeys.Add(key, new List<object>());
                            }
                            ignoreKeys[key].Add(entity.Value.GetType().GetProperty(key).GetValue(entity.Value, null));
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
            object newobject = Activator.CreateInstance(objType);
            foreach (PropertyInfo property in objType.GetProperties())
            {
                if (IsBasicType(property.PropertyType))
                {
                    object value = property.GetValue(@object, null);
                    property.SetValue(newobject, value, null);
                }
            }
            return newobject;
        }

        private static bool IsBasicType(Type type)
        {
            foreach (Type t in _basicType)
            {
                if (t == type)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ReadIgnoreKeys(Dictionary<string, List<object>> ignoreKeys, StringBuilder sql, bool hasExpression)
        {
            int i = 0;
            foreach (KeyValuePair<string, List<object>> key in ignoreKeys)
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
                sql.Append(key.Key);
                sql.Append(" not in (");
                foreach (object o in key.Value)
                {
                    if (o is string || o is char)
                    {
                        sql.Append("'");
                        sql.Append(o);
                        sql.Append("'");
                    }
                    else if (o is DateTime)
                    {
                        sql.Append("'");
                        sql.Append(((DateTime)o).ToString("yyyy-MM-dd HH:mm:ss"));
                        sql.Append("'");
                    }
                    else
                    {
                        sql.Append(o);
                    }
                    j++;
                    if (j < key.Value.Count)
                    {
                        sql.Append(",");
                    }
                }
                sql.Append(")");
            }
        }

        private static void ReadDataFromDatabase(Type objType, string sql, List<GlobalCacheResult> resultList, IPersistenceContext persistenceContext)
        {
            IDbConnection conn = persistenceContext.Connection;
            IDbDataAdapter dataAdapter = persistenceContext.DbDriverFactory.GetDbDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            DataTable dt;
            dataAdapter.Fill(ds);
            dt = ds.Tables[0];
            if (dt.Rows.Count > 0)
            {
                if (!_objTable.ContainsKey(objType))
                {
                    _objTable.Add(objType, new List<GlobalCacheEntity>());
                }
                lock (_objTable[objType])
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        object newObj = Activator.CreateInstance(objType);
                        EntityMapping mapping = persistenceContext.GetEntityMappingByClassName(objType.FullName);
                        GlobalCacheEntity cacheEntity;
                        GlobalCacheResult cacheResult;
                        foreach (PropertyInfo property in objType.GetProperties())
                        {
                            if (IsBasicType(property.PropertyType))
                            {
                                property.SetValue(newObj, row[property.Name], null);
                            }
                        }
                        cacheEntity = new GlobalCacheEntity(newObj);
                        foreach (EntityRelation relation in mapping.Relations.Values)
                        {
                            cacheEntity.ForeignKeys.Add(relation.KeyColum, row[relation.KeyColum]);
                        }
                        CheckCache();
                        _objList.Add(cacheEntity.ObjectId, cacheEntity);
                        if (!_objTable.ContainsKey(objType))
                        {
                            _objTable.Add(objType, new List<GlobalCacheEntity>());
                        }
                        _objTable[objType].Add(cacheEntity);
                        cacheResult = new GlobalCacheResult(cacheEntity.ObjectId, CloneObject(newObj), cacheEntity.Version);
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
                throw new Exception("The object do not be referenced");
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
            foreach (KeyValuePair<Guid, GlobalCacheEntity> item in _objList)
            {
                Guid objectId = item.Key;
                GlobalCacheEntity cacheEntity = item.Value;
                if (cacheEntity.ReferenceCount <= 0)
                {
                    List<GlobalCacheEntity> objectList = _objTable[cacheEntity.Value.GetType()];
                    _objList.Remove(objectId);
                    objectList.Remove(cacheEntity);
                    if (objectList.Count == 0)
                    {
                        _objTable.Remove(cacheEntity.Value.GetType());
                    }
                }
            }
            GC.Collect();
            if (_objList.Count >= CACHEMAXCOUNT)
            {
                CACHEMAXCOUNT += INCREASE;
            }
        }

        private static void CheckCache()
        {
            if (_objList.Count >= CACHEMAXCOUNT)
            {
                Collect();
            }
        }
    }
}