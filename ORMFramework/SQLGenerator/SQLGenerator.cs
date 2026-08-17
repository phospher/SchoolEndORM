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

    public IPersistenceContext PersistenceContext { get; set; }

    public SQLGenerator() { }

    public string GetSelectSQL(Type type) => GetSelectSQL(type, null);

    public string GetSelectSQL(Type type, QueryExpression queryExpression)
    {
        var sql = new StringBuilder("select * from ");
        EntityMapping mapping = PersistenceContext.GetEntityMappingByClassName(type.FullName);
        string tableName = mapping.TableName;
        sql.Append(tableName);
        List<StatElement> expression;
        if (queryExpression != null)
        {
            expression = queryExpression.NifixExpression;
        }
        else
        {
            expression = null;
        }
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
        EntityMapping mapping = PersistenceContext.GetEntityMappingByClassName(newObject.GetType().FullName);
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
                //} else if ( properties[i].PropertyType.GetInterface ( "System.Collections.ICollection" ) == null ) {
                //    object newValue = properties[i].GetValue ( newObject, null );
                //    object oldValue = properties[i].GetValue ( oldObject, null );
                //    EntityRelation relation = mapping.Relations[properties[i].Name];
                //    sql.Append ( relation.KeyColum );
                //    sql.Append ( "=" );
                //    where.Append ( relation.KeyColum );
                //    if ( newValue != null ) {
                //        sql.Append ( GetObjectPropertyValue ( newValue, relation.ReferenceColum ) );
                //    } else {
                //        sql.Append ( "NULL" );
                //    }
                //    if ( oldValue != null ) {
                //        where.Append ( "=" );
                //        where.Append ( GetObjectPropertyValue ( oldValue, relation.ReferenceColum ) );
                //    } else {
                //        where.Append ( " is NULL" );
                //    }
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
        EntityMapping mapping = PersistenceContext.GetEntityMappingByClassName(@object.GetType().FullName);
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
        EntityMapping mapping = PersistenceContext.GetEntityMappingByClassName(@object.GetType().FullName);
        string tableName = mapping.TableName;
        foreach (PropertyInfo property in @object.GetType().GetProperties())
        {
            if (IsBasicType(property.PropertyType) || IsStringType(property.PropertyType))
            {
                columns.Add(property.Name);
                values.Add(SqlLiteral.Format(property.GetValue(@object, null)));
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

        return SqlLiteral.Comparison(property.Name, property.GetValue(@object, null));
    }

    private string GenerateStatementByStringProperty(PropertyInfo property, object @object)
    {
        if (!IsStringType(property.PropertyType))
        {
            throw new ArgumentException("Invalid property type", nameof(property));
        }
        return SqlLiteral.Comparison(property.Name, property.GetValue(@object, null));
    }

    private bool IsBasicType(Type type) => _basicType.Contains(type);

    private bool IsStringType(Type type) => _stringType.Contains(type);

    private object GetObjectPropertyValue(object @object, string propertyName)
    {
        if (@object == null)
        {
            return null;
        }
        return @object.GetType().GetProperty(propertyName).GetValue(@object, null);
    }
}