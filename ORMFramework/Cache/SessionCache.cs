using System.Data;
using System.Text;
using ORMFramework.Configuration;
using ORMFramework.Exceptions;
using ORMFramework.SQL;
using ORMFramework.Statment;

namespace ORMFramework.Cache;

public sealed class SessionCache : ISessionCache
{
    private readonly Dictionary<object, CacheEntity> _objList = new();
    private readonly Dictionary<Type, List<CacheEntity>> _objTable = new();
    private readonly List<CacheEntity> _insertList = new();
    private readonly List<CacheEntity> _updateList = new();
    private readonly List<CacheEntity> _deleteList = new();

    public IPersistenceContext PersistenceContext { get; set; } = null!;

    private bool _disposed;

    public IEnumerable<object> Search(Type objType, QueryExpression? expression)
    {
        var ignoreIds = new List<Guid>();
        var result = new List<object>();
        IList<object> globalCacheObjects;
        if (_objTable.TryGetValue(objType, out var entities))
        {
            foreach (CacheEntity cacheEntity in entities)
            {
                RefreshCacheEntity(cacheEntity);
                if (ObjectCalculator.IsTargetObject(cacheEntity.Value, expression!))
                {
                    EntityMapping mapping = PersistenceContext.GetEntityMappingByClassName(objType.FullName ?? throw new OrmConfigurationException($"Type '{objType}' has no full name."))
                        ?? throw new OrmConfigurationException($"No mapping is registered for type '{objType}'.");
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

    /// <summary>
    /// 依据外键值构造谓词字符串并解析引用关系；谓词经字符串拼接进入 SQL，不提供 SQL 注入防护，调用方需自行约束输入。
    /// </summary>
    public void GetReferenceClasses(object soruceObject)
    {
        if (_objList.TryGetValue(soruceObject, out var sourceEntity))
        {
            bool hasTarget = false;
            Type objType = soruceObject.GetType();
            EntityMapping mapping = PersistenceContext.GetEntityMappingByClassName(objType.FullName ?? throw new OrmConfigurationException($"Type '{objType}' has no full name."))
                ?? throw new OrmConfigurationException($"No mapping is registered for type '{objType}'.");
            foreach (var (_, relation) in mapping.Relations)
            {
                if (sourceEntity.ForeignKeys.TryGetValue(relation.KeyColum, out var foreignKeyValue))
                {
                    Type referenceType = (objType.GetProperty(relation.Property) ?? throw new OrmConfigurationException($"Mapping references unknown property '{relation.Property}' on {objType}.")).PropertyType;
                    if (relation.Type == RelationType.ManyToOne || relation.Type == RelationType.OneToOne)
                    {
                        var expressionStr = new StringBuilder();
                        QueryExpression expression;
                        expressionStr.Append(relation.ReferenceColum);
                        expressionStr.Append("==");
                        expressionStr.Append(SqlLiteral.Format(foreignKeyValue));
                        expression = new StatementAnalyst(expressionStr.ToString()).GetQueryExpression();
                        if (_objTable.TryGetValue(referenceType, out var referenceEntities))
                        {
                            foreach (CacheEntity cacheEntity in referenceEntities)
                            {
                                RefreshCacheEntity(cacheEntity);
                                if (ObjectCalculator.IsTargetObject(cacheEntity.Value, expression))
                                {
                                    (objType.GetProperty(relation.Property) ?? throw new OrmConfigurationException($"Mapping references unknown property '{relation.Property}' on {objType}.")).SetValue(soruceObject, cacheEntity.Value);
                                    hasTarget = true;
                                    break;
                                }
                            }
                        }
                        if (!hasTarget)
                        {
                            IList<object> globalCacheResult = GetObjectsFromGlobalCache(referenceType, expression, null);
                            (objType.GetProperty(relation.Property) ?? throw new OrmConfigurationException($"Mapping references unknown property '{relation.Property}' on {objType}.")).SetValue(soruceObject, globalCacheResult[0]);
                        }
                    }
                    else
                    {
                        referenceType = (referenceType.GetGenericArguments())[0];
                        Type loadListType = typeof(LoadList<>).MakeGenericType(referenceType);
                        object loadList = Activator.CreateInstance(loadListType) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for {loadListType}");
                        var foreignKeyValues = (List<object>)(loadListType.GetProperty(nameof(LoadList<object>.ForeignKeyValues)) ?? throw new OrmConfigurationException($"Mapping references unknown property '{nameof(LoadList<object>.ForeignKeyValues)}' on {loadListType}.")).GetValue(loadList)!;
                        (loadListType.GetProperty(nameof(LoadList<object>.SessionCache)) ?? throw new OrmConfigurationException($"Mapping references unknown property '{nameof(LoadList<object>.SessionCache)}' on {loadListType}.")).SetValue(loadList, this);
                        if (relation.Type == RelationType.OneToMany)
                        {
                            foreignKeyValues.Add(foreignKeyValue);
                            (loadListType.GetProperty(nameof(LoadList<object>.ForeignKeyName)) ?? throw new OrmConfigurationException($"Mapping references unknown property '{nameof(LoadList<object>.ForeignKeyName)}' on {loadListType}.")).SetValue(loadList, relation.ReferenceColum);
                        }
                        else
                        {
                            var relation2 = (ManyToManyRelation)relation;
                            var sql = new StringBuilder("select ");
                            sql.Append(relation2.ReferenceClassColum);
                            sql.Append(" from ");
                            sql.Append(relation2.ReferenceTableName);
                            sql.Append(" where ");
                            sql.Append(SqlLiteral.Comparison(relation2.ReferenceColum, foreignKeyValue));
                            foreignKeyValues.AddRange(ProcessQueryCommand(sql.ToString()));
                            (loadListType.GetProperty(nameof(LoadList<object>.ForeignKeyName)) ?? throw new OrmConfigurationException($"Mapping references unknown property '{nameof(LoadList<object>.ForeignKeyName)}' on {loadListType}.")).SetValue(loadList, relation2.ReferenceClassKeyColum);
                        }
                        (objType.GetProperty(relation.Property) ?? throw new OrmConfigurationException($"Mapping references unknown property '{relation.Property}' on {objType}.")).SetValue(soruceObject, loadList);
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
            _updateList.Remove(cacheEntity);
            if (!_insertList.Remove(cacheEntity))
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
        catch
        {
            RollBack();
            throw;
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
        catch
        {
            RollBack();
            throw;
        }
    }

    public void Insert(object @object)
    {
        try
        {
            if (_objList.ContainsKey(@object))
            {
                throw new InvalidOperationException("The object exists");
            }
            else
            {
                var cacheEntity = new CacheEntity();
                cacheEntity.ObjectId = Guid.NewGuid();
                cacheEntity.Value = @object;
                cacheEntity.Version = DateTime.UtcNow.Ticks;
                _objList.Add(cacheEntity.Value, cacheEntity);
                if (!_objTable.TryGetValue(cacheEntity.Value.GetType(), out var list))
                {
                    list = [];
                    _objTable[cacheEntity.Value.GetType()] = list;
                }
                list.Add(cacheEntity);
                _insertList.Add(cacheEntity);
            }
        }
        catch
        {
            RollBack();
            throw;
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
            if (!_objTable.TryGetValue(cacheEntity.Value.GetType(), out var list))
            {
                list = [];
                _objTable[cacheEntity.Value.GetType()] = list;
            }
            list.Add(cacheEntity);
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
        GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId) ?? throw new ConcurrencyException("object has been deleted");
        _objList.Remove(cacheEntity.Value);
        cacheEntity.Value = globalCacheResult.Value;
        cacheEntity.Version = globalCacheResult.Version;
        _objList.Add(cacheEntity.Value, cacheEntity);
    }

    private List<object> ProcessQueryCommand(string sql)
    {
        var result = new List<object>();
        IDbDriverFactory driverFactory = PersistenceContext.DbDriverFactory;
        using var conn = driverFactory.GetDbConnection();
        using var ds = new DataSet();
        IDbDataAdapter da = driverFactory.GetDbDataAdapter(sql, conn);
        using (da as IDisposable)
        {
            da.Fill(ds);
        }
        DataTable dt = ds.Tables[0];
        foreach (DataRow row in dt.Rows)
        {
            result.Add(row[0]);
        }
        return result;
    }

    public void Commit()
    {
        IDbDriverFactory driverFactory = PersistenceContext.DbDriverFactory;
        using var conn = driverFactory.GetDbConnection();
        conn.Open();
        using var tran = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tran;
        try
        {
            CommitToDB(cmd);
            CommitToGlobalCache();
            ClearAllChangeList();
            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
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
            GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId) ?? throw new ConcurrencyException("object has been deleted");
            string sql = PersistenceContext.SQLGenerator.GetUpdateSQL(globalCacheResult.Value, cacheEntity.Value);
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    private void CommitUpdateListToGlobalCache()
    {
        foreach (CacheEntity cacheEntity in _updateList)
        {
            GlobalCache.Update(cacheEntity.ObjectId, cacheEntity.Value, cacheEntity.ForeignKeys, PersistenceContext);
        }
    }

    private void CommitDeleteListToDB(IDbCommand cmd)
    {
        foreach (CacheEntity cacheEntity in _deleteList)
        {
            GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId) ?? throw new ConcurrencyException("object has been deleted");
            string sql = PersistenceContext.SQLGenerator.GetDeleteSQL(globalCacheResult.Value);
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
            string sql = PersistenceContext.SQLGenerator.GetInsertSQL(cacheEntity.Value);
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

    private static void RefreshCacheEntity(CacheEntity cacheEntity)
    {
        if (GlobalCache.GetObjectVersion(cacheEntity.ObjectId) > cacheEntity.Version)
        {
            GlobalCacheResult globalCacheResult = GlobalCache.Search(cacheEntity.ObjectId) ?? throw new ConcurrencyException("object has been deleted");
            cacheEntity.Value = globalCacheResult.Value;
            cacheEntity.Version = globalCacheResult.Version;
            cacheEntity.ForeignKeys = globalCacheResult.ForeignKeys;
        }
    }

    private IList<object> GetObjectsFromGlobalCache(Type objType, QueryExpression? expression, IList<Guid>? ignoreIds)
    {
        var result = new List<object>();
        IEnumerable<GlobalCacheResult> globalCacheResults;
        ignoreIds ??= [];
        foreach (CacheEntity cacheEntity in _deleteList)
        {
            ignoreIds.Add(cacheEntity.ObjectId);
        }
        globalCacheResults = GlobalCache.Search(objType, expression, PersistenceContext, ignoreIds);
        foreach (GlobalCacheResult globalCacheResult in globalCacheResults)
        {
            var cacheEntity = new CacheEntity();
            cacheEntity.ObjectId = globalCacheResult.ObjectId;
            cacheEntity.Value = globalCacheResult.Value;
            cacheEntity.Version = globalCacheResult.Version;
            cacheEntity.ForeignKeys = globalCacheResult.ForeignKeys;
            _objList.Add(cacheEntity.Value, cacheEntity);
            if (!_objTable.TryGetValue(objType, out var list))
            {
                list = [];
                _objTable[objType] = list;
            }
            list.Add(cacheEntity);
            result.Add(cacheEntity.Value);
        }
        return result;
    }

    private void Collect()
    {
        foreach (CacheEntity entity in _objList.Values)
        {
            GlobalCache.ReleaseReferenct(entity.ObjectId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Collect();
    }
}