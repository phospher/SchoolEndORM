using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ORMFramework.Configuration;
using ORMFramework.Statment;

namespace ORMFramework.SQL
{
    public class SQLGenerator : ISQLGenerator
    {
        private static readonly string[] _sqlOperators = new string[] { "=", "!=", ">", ">=", "<", "<=", " and ", " or ", " like ", "(", ")" };
        private static readonly Type[] _basicType = new Type[] {
            typeof (bool), typeof (byte), typeof (sbyte),
            typeof (decimal), typeof (double), typeof (float), typeof (int), typeof (uint), typeof (long),
            typeof (ulong), typeof (short), typeof (ushort), typeof (byte[])
        };
        private static readonly Type[] _stringType = new Type[] { typeof(char), typeof(string), typeof(DateTime) };
        private IPersistenceContext _persistenceContext;

        public IPersistenceContext PersistenceContext
        {
            get { return _persistenceContext; }
            set { _persistenceContext = value; }
        }

        public SQLGenerator() { }

        public SQLGenerator(IPersistenceContext pesistenceContext)
        {
            _persistenceContext = pesistenceContext;
        }

        public string GetSelectSQL(Type type)
        {
            return GetSelectSQL(type, null);
        }

        public string GetSelectSQL(Type type, QueryExpression queryExpression)
        {
            List<StatElement> expression;
            StringBuilder sql = new StringBuilder("select * from ");
            EntityMapping mapping = _persistenceContext.GetEntityMappingByClassName(type.FullName);
            string tableName = mapping.TableName;
            sql.Append(tableName);
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
                            throw new Exception("Invalid Statement");
                    }
                }
            }
            sql.Append(")");
            return sql.ToString();
        }

        public string GetUpdateSQL(object oldObject, object newObject)
        {
            if (oldObject.GetType() != newObject.GetType())
            {
                throw new Exception("Type of oldOject must be the same as that of newObject");
            }

            StringBuilder sql = new StringBuilder("update ");
            StringBuilder where = new StringBuilder(" where (");
            EntityMapping mapping = _persistenceContext.GetEntityMappingByClassName(newObject.GetType().FullName);
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
            StringBuilder sql = new StringBuilder("delete from ");
            EntityMapping mapping = _persistenceContext.GetEntityMappingByClassName(@object.GetType().FullName);
            string tableName = mapping.TableName;
            PropertyInfo[] properties = @object.GetType().GetProperties();
            sql.Append(tableName);
            sql.Append(" where (");
            for (int i = 0; i < properties.Length; i++)
            {
                if (IsBasicType(properties[i].PropertyType))
                {
                    sql.Append(GenerateStatmentByProperty(properties[i], @object));
                }
                else if (IsStringType(properties[i].PropertyType))
                {
                    sql.Append(GenerateStatementByStringProperty(properties[i], @object));
                    //} else if ( properties[i].PropertyType.GetInterface ( "System.Collections.ICollection" ) == null ) {
                    //    object value = properties[i].GetValue ( @object, null );
                    //    EntityRelation relation = mapping.Relations[properties[i].Name];
                    //    sql.Append ( relation.KeyColum );
                    //    if ( value != null ) {
                    //        sql.Append ( "=" );
                    //        sql.Append ( GetObjectPropertyValue ( value, relation.ReferenceColum ) );
                    //    } else {
                    //        sql.Append ( " is NULL" );
                    //    }
                }
                else
                {
                    continue;
                }
                if (i < properties.Length - 1)
                {
                    sql.Append(" and ");
                }
            }
            sql.Append(")");
            return sql.ToString();
        }

        public string GetInsertSQL(object @object)
        {
            StringBuilder sql = new StringBuilder("insert into ");
            StringBuilder colums = new StringBuilder("(");
            StringBuilder values = new StringBuilder("(");
            EntityMapping mapping = _persistenceContext.GetEntityMappingByClassName(@object.GetType().FullName);
            string tableName = mapping.TableName;
            PropertyInfo[] properties = @object.GetType().GetProperties();
            sql.Append(tableName);
            for (int i = 0; i < properties.Length; i++)
            {
                if (IsBasicType(properties[i].PropertyType))
                {
                    object value = properties[i].GetValue(@object, null);
                    colums.Append(properties[i].Name);
                    if (value != null)
                    {
                        values.Append(value);
                    }
                    else
                    {
                        values.Append("NULL");
                    }
                }
                else if (IsStringType(properties[i].PropertyType))
                {
                    object value = properties[i].GetValue(@object, null);
                    colums.Append(properties[i].Name);
                    if (value == null)
                    {
                        values.Append("NULL");
                    }
                    else if (value.GetType() == typeof(DateTime))
                    {
                        values.Append(string.Format("'{0}'", ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss")));
                    }
                    else
                    {
                        values.Append(string.Format("'{0}'", value));
                    }
                    //} else if ( properties[i].PropertyType.GetInterface ( "System.Collections.ICollection" ) == null ) {
                    //    object value = properties[i].GetValue ( @object, null );
                    //    EntityRelation relation = mapping.Relations[properties[i].Name];
                    //    colums.Append ( relation.KeyColum );
                    //    if ( value != null ) {
                    //        values.Append ( GetObjectPropertyValue ( value, relation.ReferenceColum ) );
                    //    } else {
                    //        values.Append ( "NULL" );
                    //    }
                }
                else
                {
                    continue;
                }
                if (i < properties.Length - 1)
                {
                    colums.Append(",");
                    values.Append(",");
                }
            }
            colums.Append(")");
            values.Append(")");
            sql.Append(colums);
            sql.Append(" values");
            sql.Append(values);
            return sql.ToString();
        }

        private string GetSQLOperator(Operator @operator)
        {
            return _sqlOperators[(int)@operator];
        }

        private string GenerateStatmentByProperty(PropertyInfo property, object @object)
        {
            if (!IsBasicType(property.PropertyType))
            {
                throw new Exception("Invalid property type");
            }

            return string.Format("{0}={1}", property.Name, property.GetValue(@object, null));
        }

        private string GenerateStatementByStringProperty(PropertyInfo property, object @object)
        {
            if (!IsStringType(property.PropertyType))
            {
                throw new Exception("Invalid property type");
            }
            object value = property.GetValue(@object, null);
            if (value == null)
            {
                return string.Format("{0}='{1}'", property.Name, "NULL");
            }
            else if (value.GetType() == typeof(DateTime))
            {
                return string.Format("{0}='{1}'", property.Name, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                return string.Format("{0}='{1}'", property.Name, value);
            }
        }

        private bool IsBasicType(Type type)
        {
            for (int i = 0; i < _basicType.Length; i++)
            {
                if (type == _basicType[i])
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsStringType(Type type)
        {
            for (int i = 0; i < _stringType.Length; i++)
            {
                if (type == _stringType[i])
                {
                    return true;
                }
            }
            return false;
        }

        private object GetObjectPropertyValue(object @object, string propertyName)
        {
            if (@object == null)
            {
                return null;
            }
            return @object.GetType().GetProperty(propertyName).GetValue(@object, null);
        }
    }
}