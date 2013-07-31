using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Statment;
using ORMFramework.Configuration;
using System.Reflection;
using System.Data;
using ORMFramework.SQL;

namespace ORMFramework.Cache
{
    public class SessionCache : ISessionCache
    {
        private Dictionary<object, CacheEntity> _objList = new Dictionary<object, CacheEntity>();
        private Dictionary<Type, List<CacheEntity>> _objTable = new Dictionary<Type, List<CacheEntity>>();
        private List<CacheEntity> _insertList = new List<CacheEntity>();
        private List<CacheEntity> _updateList = new List<CacheEntity>();
        private List<CacheEntity> _deleteList = new List<CacheEntity>();
        private IPersistenceContext _persistenceContext;
        private ISQLGenerator _sqlGenerator;

        public IPersistenceContext PersistenceContext
        {
            get { return _persistenceContext; }
        }

        public SessionCache(IPersistenceContext persistenceContext)
        {
            _persistenceContext = persistenceContext;
            _sqlGenerator = new SQLGenerator(_persistenceContext);
        }

        ~SessionCache()
        {
            Collect();
        }

        public IEnumerable<object> Search(Type objType, QueryExpression expression)
        {
            List<Guid> ignoreIds = new List<Guid>();
            List<object> result = new List<object>();
            IList<object> globalCacheObjects;
            if (_objTable.ContainsKey(objType))
            {
                foreach (CacheEntity cacheEntity in _objTable[objType])
                {
                    RefreshCacheEntity(cacheEntity);
                    if (ObjectCalculator.IsTargetObject(cacheEntity.Value, expression))
                    {
                        EntityMapping mapping = _persistenceContext.GetEntityMappingByClassName(objType.FullName);
                        result.Add((object)cacheEntity.Value);
                        ignoreIds.Add(cacheEntity.ObjectId);
                    }
                }
            }
            globalCacheObjects = GetObjectsFromGlobalCache(objType, expression, ignoreIds);
            foreach (object o in globalCacheObjects)
            {
                GetReferenceClasses(o);
                result.Add(o);
            }
            return result;
        }

        public void GetReferenceClasses(object soruceObject)
        {
            if (_objList.ContainsKey(soruceObject))
            {
                bool hasTarget = false;
                Type objType = soruceObject.GetType();
                EntityMapping mapping = _persistenceContext.GetEntityMappingByClassName(objType.FullName);
                foreach (KeyValuePair<string, EntityRelation> item in mapping.Relations)
                {
                    EntityRelation relation = item.Value;
                    if (_objList[soruceObject].ForeignKeys.ContainsKey(relation.KeyColum))
                    {
                        Type referenceType = objType.GetProperty(relation.Property).PropertyType;
                        if (relation.Type == RelationType.ManyToOne || relation.Type == RelationType.OneToOne)
                        {
                            StringBuilder expressionStr = new StringBuilder();
                            QueryExpression expression;
                            object foreignKeysValue = _objList[soruceObject].ForeignKeys[relation.KeyColum];
                            expressionStr.Append(relation.ReferenceColum);
                            expressionStr.Append("==");
                            if (foreignKeysValue is string || foreignKeysValue is char)
                            {
                                expressionStr.Append(string.Format("'{0}'", foreignKeysValue));
                            }
                            else if (foreignKeysValue is DateTime)
                            {
                                expressionStr.Append(string.Format("'{0}'", ((DateTime)foreignKeysValue).ToString("yyyy-MM-dd HH:mm:ss")));
                            }
                            else
                            {
                                expressionStr.Append(foreignKeysValue);
                            }
                            expression = new StatementAnalyst(expressionStr.ToString()).GetQueryExpression();
                            if (_objTable.ContainsKey(referenceType))
                            {
                                foreach (CacheEntity cacheEntity in _objTable[referenceType])
                                {
                                    RefreshCacheEntity(cacheEntity);
                                    if (ObjectCalculator.IsTargetObject(cacheEntity.Value, expression))
                                    {
                                        objType.GetProperty(relation.Property).SetValue(soruceObject, cacheEntity.Value, null);
                                        hasTarget = true;
                                        break;
                                    }
                                }
                            }
                            if (!hasTarget)
                            {
                                IList<object> globalCacheResult = GetObjectsFromGlobalCache(referenceType, expression, null);
                                objType.GetProperty(relation.Property).SetValue(soruceObject, globalCacheResult[0], null);
                            }
                        }
                        else
                        {
                            referenceType = (referenceType.GetGenericArguments())[0];
                            Type loadListType = Assembly.Load("ORMFramework").GetType("ORMFramework.LoadList`1").MakeGenericType(referenceType);
                            object loadList = Activator.CreateInstance(loadListType);
                            List<object> foreignKeyValues = (List<object>)loadListType.GetProperty("ForeignKeyValues").GetValue(loadList, null);
                            loadListType.GetProperty("SessionCache").SetValue(loadList, this, null);
                            if (relation.Type == RelationType.OneToMany)
                            {
                                foreignKeyValues.Add(_objList[soruceObject].ForeignKeys[relation.KeyColum]);
                                loadListType.GetProperty("ForeignKeyName").SetValue(loadList, relation.ReferenceColum, null);
                            }
                            else
                            {
                                ManyToManyRelation relation2 = (ManyToManyRelation)relation;
                                StringBuilder sql = new StringBuilder("select ");
                                object foreingKeyValue = _objList[soruceObject].ForeignKeys[relation2.KeyColum];
                                sql.Append(relation2.ReferenceClassColum);
                                sql.Append(" from ");
                                sql.Append(relation2.ReferenceTableName);
                                sql.Append(" where ");
                                sql.Append(relation2.ReferenceColum);
                                sql.Append("=");
                                if (foreingKeyValue is string || foreingKeyValue is char)
                                {
                                    sql.Append(string.Format("'{0}'", foreingKeyValue));
                                }
                                else if (foreingKeyValue is DateTime)
                                {
                                    sql.Append(string.Format("'{0}'", ((DateTime)foreingKeyValue).ToString("yyyy-MM-dd HH:mm:ss")));
                                }
                                else
                                {
                                    sql.Append(foreingKeyValue);
                                }
                                foreignKeyValues.AddRange(ProcessQueryCommand(sql.ToString()));
                                loadListType.GetProperty("ForeignKeyName").SetValue(loadList, relation2.ReferenceClassKeyColum, null);
                            }
                            objType.GetProperty(relation.Property).SetValue(soruceObject, loadList, null);
                        }
                    }
                }
            }
        }

        public void Delete(object @object)
        {
            try
            {
                CacheEntity cacheEntity = _objList[@object];
                GlobalCache.GetWriteLockForObject(cacheEntity.ObjectId);
                if (_updateList.Contains(cacheEntity))
                {
                    _updateList.Remove(cacheEntity);
                }
                if (_insertList.Contains(cacheEntity))
                {
                    _insertList.Remove(cacheEntity);
                }
                else
                {
                    _deleteList.Add(cacheEntity);
                }
                _objList.Remove(@object);
                _objTable[@object.GetType()].Remove(cacheEntity);
                if (_objTable[@object.GetType()].Count == 0)
                {
                    _objTable.Remove(@object.GetType());
                }
            }
            catch (Exception ex)
            {
                RollBack();
                throw ex;
            }
        }

        public void Update(object @object)
        {
            try
            {
                CacheEntity cacheEntity = _objList[@object];
                GlobalCache.GetWriteLockForObject(cacheEntity.ObjectId);
                cacheEntity.Version = DateTime.UtcNow.Ticks;
                if (!_insertList.Contains(cacheEntity))
                {
                    _updateList.Add(cacheEntity);
                }
            }
            catch (Exception ex)
            {
                RollBack();
                throw ex;
            }
        }

        public void Insert(object @object)
        {
            try
            {
                if (_objList.ContainsKey(@object))
                {
                    throw new Exception("The object exists");
                }
                else
                {
                    CacheEntity cacheEntity = new CacheEntity();
                    cacheEntity.ObjectId = Guid.NewGuid();
                    cacheEntity.Value = @object;
                    cacheEntity.Version = DateTime.UtcNow.Ticks;
                    _objList.Add(cacheEntity.Value, cacheEntity);
                    if (!_objTable.ContainsKey(cacheEntity.Value.GetType()))
                    {
                        _objTable.Add(cacheEntity.Value.GetType(), new List<CacheEntity>());
                    }
                    _objTable[cacheEntity.Value.GetType()].Add(cacheEntity);
                    _insertList.Add(cacheEntity);
                }
            }
            catch (Exception ex)
            {
                RollBack();
                throw ex;
            }
        }

        private void RollBack()
        {
            foreach (CacheEntity cacheEntity in _updateList)
            {
                ResetEntity(cacheEntity);
                GlobalCache.ReleaseWriteLock(cacheEntity.ObjectId);
            }
            foreach (CacheEntity cacheEntity in _deleteList)
            {
                ResetEntity(cacheEntity);
                if (!_objTable.ContainsKey(cacheEntity.Value.GetType()))
                {
                    _objTable.Add(cacheEntity.Value.GetType(), new List<CacheEntity>());
                }
                _objTable[cacheEntity.Value.GetType()].Add(cacheEntity);
                GlobalCache.ReleaseWriteLock(cacheEntity.ObjectId);
            }
            foreach (CacheEntity cacheEntity in _insertList)
            {
                _objList.Remove(cacheEntity.Value);
                _objTable[cacheEntity.Value.GetType()].Remove(cacheEntity);
                if (_objTable[cacheEntity.Value.GetType()].Count == 0)
                {
                    _objTable.Remove(cacheEntity.Value.GetType());
                }
            }
            _insertList.Clear();
            _deleteList.Clear();
            _updateList.Clear();
        }

        private void ResetEntity(CacheEntity cacheEntity)
        {
            GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId);
            if (_objList.ContainsKey(cacheEntity.Value))
            {
                _objList.Remove(cacheEntity.Value);
            }
            cacheEntity.Value = globalCacheResult.Value;
            cacheEntity.Version = globalCacheResult.Version;
            _objList.Add(cacheEntity.Value, cacheEntity);
        }

        private List<object> ProcessQueryCommand(string sql)
        {
            List<object> result = new List<object>();
            IDbDriverFactory driverFactory = _persistenceContext.DbDriverFactory;
            IDbConnection conn = driverFactory.GetDbConnection();
            IDbDataAdapter da = driverFactory.GetDbDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            DataTable dt;
            da.Fill(ds);
            dt = ds.Tables[0];
            foreach (DataRow row in dt.Rows)
            {
                result.Add(row[0]);
            }
            return result;
        }

        public void Commit()
        {
            IDbDriverFactory driverFactory = _persistenceContext.DbDriverFactory;
            IDbConnection conn = driverFactory.GetDbConnection();
            IDbCommand cmd;
            IDbTransaction tran;
            conn.Open();
            tran = conn.BeginTransaction();
            cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            try
            {
                CommitToDB(cmd);
                CommitToGlobalCache();
                ClearAllChangeList();
                tran.Commit();
                conn.Close();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                conn.Close();
                throw ex;
            }
        }

        private void CommitToDB(IDbCommand cmd)
        {
            CommitDeleteListToDB(cmd);
            CommitUpdateListToDB(cmd);
            CommitInsertListToDB(cmd);
        }

        private void CommitToGlobalCache()
        {
            CommitUpdateListToGlobalCache();
            CommitInsertListToGlobalCache();
        }

        private void ClearAllChangeList()
        {
            _updateList.Clear();
            _insertList.Clear();
            _deleteList.Clear();
        }

        private void CommitUpdateListToDB(IDbCommand cmd)
        {
            foreach (CacheEntity cacheEntity in _updateList)
            {
                GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId);
                string sql = _sqlGenerator.GetUpdateSQL(globalCacheResult.Value, cacheEntity.Value);
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private void CommitUpdateListToGlobalCache()
        {
            foreach (CacheEntity cacheEntity in _updateList)
            {
                GlobalCache.Update(cacheEntity.ObjectId, cacheEntity.Value, cacheEntity.ForeignKeys, _persistenceContext);
            }
        }

        private void CommitDeleteListToDB(IDbCommand cmd)
        {
            foreach (CacheEntity cacheEntity in _deleteList)
            {
                GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId);
                string sql = _sqlGenerator.GetDeleteSQL(globalCacheResult.Value);
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                GlobalCache.Delete(cacheEntity.ObjectId);
            }
            _deleteList.Clear();
        }

        private void CommitInsertListToDB(IDbCommand cmd)
        {
            foreach (CacheEntity cacheEntity in _insertList)
            {
                string sql = _sqlGenerator.GetInsertSQL(cacheEntity.Value);
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private void CommitInsertListToGlobalCache()
        {
            foreach (CacheEntity item in _insertList)
            {
                GlobalCache.Insert(item);
            }
        }

        private void RefreshCacheEntity(CacheEntity cacheEntity)
        {
            if (GlobalCache.GetObjectVersion(cacheEntity.ObjectId) > cacheEntity.Version)
            {
                GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId);
                cacheEntity.Value = globalCacheResult.Value;
                cacheEntity.Version = globalCacheResult.Version;
                cacheEntity.ForeignKeys = globalCacheResult.ForeignKeys;
            }
        }

        private IList<object> GetObjectsFromGlobalCache(Type objType, QueryExpression expression, IList<Guid> ignoreIds)
        {
            List<object> result = new List<object>();
            IEnumerable<GlobalCacheResult> globalCacheResults;
            foreach (CacheEntity cacheEntity in _deleteList)
            {
                ignoreIds.Add(cacheEntity.ObjectId);
            }
            globalCacheResults = GlobalCache.Search(objType, expression, _persistenceContext, ignoreIds);
            foreach (GlobalCacheResult globalCacheResult in globalCacheResults)
            {
                CacheEntity cacheEntity = new CacheEntity();
                cacheEntity.ObjectId = globalCacheResult.ObjectId;
                cacheEntity.Value = globalCacheResult.Value;
                cacheEntity.Version = globalCacheResult.Version;
                cacheEntity.ForeignKeys = globalCacheResult.ForeignKeys;
                _objList.Add(cacheEntity.Value, cacheEntity);
                if (!_objTable.ContainsKey(objType))
                {
                    _objTable.Add(objType, new List<CacheEntity>());
                }
                _objTable[objType].Add(cacheEntity);
                result.Add(cacheEntity.Value);
            }
            return result;
        }

        private void Collect()
        {
            foreach (Guid objectId in _objList.Keys)
            {
                GlobalCache.ReleaseReferenct(objectId);
            }
        }

        public void Dispose()
        {
            Collect();
            GC.SuppressFinalize(this);
        }
    }
}
