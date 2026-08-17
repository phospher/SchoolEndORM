using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using ORMFramework.Configuration;
using ORMFramework.Exceptions;
using ORMFramework.Statment;

namespace ORMFramework.SQL;

/// <summary>
/// 通过字符串拼接生成 SQL；不使用参数化查询，不提供 SQL 注入防护，调用方需自行约束输入。
/// </summary>
public sealed class SQLGenerator : ISQLGenerator
{
    private static readonly string[] _sqlOperators = ["=", "!=", ">", ">=", "<", "<=", " and ", " or ", " like ", "(", ")"];
    private static readonly FrozenSet<Type> _basicType = FrozenSet.ToFrozenSet(
    [
        typeof(bool), typeof(byte), typeof(sbyte),
        typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint), typeof(long),
        typeof(ulong), typeof(short), typeof(ushort), typeof(byte[])
    ]);
    private static readonly FrozenSet<Type> _stringType = FrozenSet.ToFrozenSet([typeof(char), typeof(string), typeof(DateTime)]);

    public IPersistenceContext? PersistenceContext { get; set; }

    public SQLGenerator() { }

    private IPersistenceContext PersistenceContextOrThrow =>
        PersistenceContext ?? throw new InvalidOperationException("PersistenceContext is not configured");

    public string GetSelectSQL(Type type) => GetSelectSQL(type, null);

    public string GetSelectSQL(Type type, QueryExpression? queryExpression)
    {
        var sql = new StringBuilder("select * from ");
        EntityMapping mapping = PersistenceContextOrThrow.GetEntityMappingByClassName(type.FullName ?? throw new OrmConfigurationException($"Type '{type}' has no full name."))
            ?? throw new OrmConfigurationException($"No mapping is registered for type '{type}'.");
        string tableName = mapping.TableName;
        sql.Append(tableName);
        List<StatElement>? expression = queryExpression?.NifixExpression;
        if (expression != null && expression.Count > 0)
        {
            sql.Append(" where (");
            foreach (StatElement element in expression)
            {
                switch (element.Type)
                {
                    case StatElementType.Constant:
                        sql.Append(element.Value);
                        break;
                    case StatElementType.Operator:
                        sql.Append(GetSQLOperator((Operator)element.Value));
                        break;
                    case StatElementType.Variable:
                        sql.Append(element.Value);
                        break;
                    default:
                        throw new QuerySyntaxException("Invalid Statement");
                }
            }
            sql.Append(")");
        }
        return sql.ToString();
    }

    public string GetUpdateSQL(object oldObject, object newObject)
    {
        ArgumentNullException.ThrowIfNull(oldObject);
        ArgumentNullException.ThrowIfNull(newObject);
        if (oldObject.GetType() != newObject.GetType())
        {
            throw new ArgumentException("Type of oldOject must be the same as that of newObject", nameof(newObject));
        }

        var sql = new StringBuilder("update ");
        var where = new StringBuilder(" where (");
        EntityMapping mapping = PersistenceContextOrThrow.GetEntityMappingByClassName(newObject.GetType().FullName ?? throw new OrmConfigurationException($"Type '{newObject.GetType()}' has no full name."))
            ?? throw new OrmConfigurationException($"No mapping is registered for type '{newObject.GetType()}'.");
        string tableName = mapping.TableName;
        PropertyInfo[] properties = oldObject.GetType().GetProperties();
        sql.Append(tableName);
        sql.Append(" set ");
        for (int i = 0; i < properties.Length; i++)
        {
            if (IsBasicType(properties[i].PropertyType))
            {
                sql.Append(GenerateStatmentByProperty(properties[i], newObject));
                where.Append(GenerateStatmentByProperty(properties[i], oldObject));
            }
            else if (IsStringType(properties[i].PropertyType))
            {
                sql.Append(GenerateStatementByStringProperty(properties[i], newObject));
                where.Append(GenerateStatementByStringProperty(properties[i], oldObject));
            }
            else
            {
                continue;
            }
            sql.Append(",");
            where.Append(" and ");
        }
        sql.Remove(sql.Length - 1, 1);
        where.Remove(where.Length - 5, 5);
        where.Append(")");
        sql.Append(where);
        return sql.ToString();
    }

    public string GetDeleteSQL(object @object)
    {
        var parts = new List<string>();
        EntityMapping mapping = PersistenceContextOrThrow.GetEntityMappingByClassName(@object.GetType().FullName ?? throw new OrmConfigurationException($"Type '{@object.GetType()}' has no full name."))
            ?? throw new OrmConfigurationException($"No mapping is registered for type '{@object.GetType()}'.");
        string tableName = mapping.TableName;
        foreach (PropertyInfo property in @object.GetType().GetProperties())
        {
            if (IsBasicType(property.PropertyType))
            {
                parts.Add(GenerateStatmentByProperty(property, @object));
            }
            else if (IsStringType(property.PropertyType))
            {
                parts.Add(GenerateStatementByStringProperty(property, @object));
            }
        }
        return $"delete from {tableName} where ({string.Join(" and ", parts)})";
    }

    public string GetInsertSQL(object @object)
    {
        var columns = new List<string>();
        var values = new List<string>();
        EntityMapping mapping = PersistenceContextOrThrow.GetEntityMappingByClassName(@object.GetType().FullName ?? throw new OrmConfigurationException($"Type '{@object.GetType()}' has no full name."))
            ?? throw new OrmConfigurationException($"No mapping is registered for type '{@object.GetType()}'.");
        string tableName = mapping.TableName;
        foreach (PropertyInfo property in @object.GetType().GetProperties())
        {
            if (IsBasicType(property.PropertyType) || IsStringType(property.PropertyType))
            {
                columns.Add(property.Name);
                values.Add(SqlLiteral.Format(property.GetValue(@object)));
            }
        }
        return $"insert into {tableName}({string.Join(",", columns)}) values({string.Join(",", values)})";
    }

    private static string GetSQLOperator(Operator @operator) => _sqlOperators[(int)@operator];

    private string GenerateStatmentByProperty(PropertyInfo property, object @object)
    {
        if (!IsBasicType(property.PropertyType))
        {
            throw new ArgumentException("Invalid property type", nameof(property));
        }

        return SqlLiteral.Comparison(property.Name, property.GetValue(@object));
    }

    private string GenerateStatementByStringProperty(PropertyInfo property, object @object)
    {
        if (!IsStringType(property.PropertyType))
        {
            throw new ArgumentException("Invalid property type", nameof(property));
        }
        return SqlLiteral.Comparison(property.Name, property.GetValue(@object));
    }

    private bool IsBasicType(Type type) => _basicType.Contains(type);

    private bool IsStringType(Type type) => _stringType.Contains(type);
}