using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace ORMFramework.Configuration
{
    public class ConfigManager
    {
        private string _configFilePath;

        public string ConfigFilePath
        {
            get { return _configFilePath; }
            set { _configFilePath = value; }
        }

        public ConfigManager()
        {
            _configFilePath = Environment.CurrentDirectory + "\\Config.xml";
        }

        public ConfigManager(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        public Configuration GetSystemConfiguration()
        {
            FileStream fs = new FileStream(_configFilePath, FileMode.Open);
            XmlTextReader reader = new XmlTextReader(fs);
            Configuration configuration = new Configuration();
            bool result = true;
            reader.WhitespaceHandling = WhitespaceHandling.None;
            if (reader.Read() && reader.NodeType == XmlNodeType.XmlDeclaration)
            {
                if (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == "Configuration")
                {
                    while (result && reader.Read() && reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "SessionFactory":
                                result = ReadSessionFactory(reader, configuration);
                                break;
                            case "Mappings":
                                result = ReadMappings(reader, configuration);
                                break;
                            default:
                                break;
                        }
                    }
                    reader.Close();
                    fs.Close();
                    if (result)
                    {
                        return configuration;
                    }
                    else
                    {
                        reader.Close();
                        fs.Close();
                        throw new Exception("Invail Config File");
                    }
                }
                else
                {
                    reader.Close();
                    fs.Close();
                    throw new Exception("Invail Config File");
                }
            }
            else
            {
                reader.Close();
                fs.Close();
                throw new Exception("Invail Config File");
            }
        }

        private bool ReadSessionFactory(XmlTextReader reader, Configuration configuration)
        {
            bool result = true;
            while (result && reader.Read() && reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "Database":
                        result = ReadDatabase(reader, configuration);
                        break;
                    case "ConnectionString":
                        result = ReadConnectionString(reader, configuration);
                        break;
                    case "ProviderName":
                        result = ReadProviderName(reader, configuration);
                        break;
                    default:
                        return false;
                }
            }
            if (result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "SessionFactory")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReadDatabase(XmlTextReader reader, Configuration configuration)
        {
            bool result = false;
            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
            {
                switch (reader.ReadString().ToLower())
                {
                    case "sqlserver":
                        configuration.DatabaseType = DatabaseType.SQLServer;
                        result = true;
                        break;
                    case "odbc":
                        configuration.DatabaseType = DatabaseType.Odbc;
                        result = true;
                        break;
                    case "oledb":
                        configuration.DatabaseType = DatabaseType.OleDb;
                        result = true;
                        break;
                    case "oracle":
                        configuration.DatabaseType = DatabaseType.Oracle;
                        break;
                    default:
                        return false;
                }
            }
            if (result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "Database")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReadConnectionString(XmlTextReader reader, Configuration configuration)
        {
            bool result = false;
            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
            {
                configuration.ConnectionString = reader.ReadString().Trim();
                result = true;
            }
            if (result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "ConnectionString")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReadProviderName(XmlTextReader reader, Configuration configuration)
        {
            bool result = false;
            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
            {
                configuration.ProviderName = reader.ReadString().Trim();
                result = true;
            }
            if (result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "ProviderName")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReadMappings(XmlTextReader reader, Configuration configuration)
        {
            bool result = true;
            while (result && reader.Read() && reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "Map")
                {
                    result = ReadMap(reader, configuration);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool ReadMap(XmlTextReader reader, Configuration configuration)
        {
            XmlDocument xmlDoc = new XmlDocument();
            EntityMapping map = new EntityMapping();
            XmlNode mapNode;
            xmlDoc.Load(reader.ReadSubtree());
            mapNode = xmlDoc.ChildNodes.Item(0);
            map.ClassName = mapNode.Attributes["ClassName"].Value;
            map.Keys = mapNode.Attributes["Key"].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            map.TableName = mapNode.Attributes["TableName"].Value;
            for (int i = 0; i < mapNode.ChildNodes.Count; i++)
            {
                XmlNode node = mapNode.ChildNodes.Item(i);
                EntityRelation relation;
                if (node.Name == "Many-to-Many")
                {
                    relation = GetManyToManyRelation(node);
                    if (relation == null)
                    {
                        return false;
                    }
                }
                else
                {
                    relation = new EntityRelation();
                    switch (node.Name)
                    {
                        case "One-to-Many":
                            relation.Type = RelationType.OneToMany;
                            break;
                        case "One-to-One":
                            relation.Type = RelationType.OneToOne;
                            break;
                        case "Many-to-One":
                            relation.Type = RelationType.ManyToOne;
                            break;
                        default:
                            return false;
                    }
                    relation.KeyColum = node.Attributes["KeyColum"].Value;
                    relation.Property = node.Attributes["Property"].Value;
                    relation.ReferenceClassName = node.Attributes["ReferenceClass"].Value;
                    relation.ReferenceColum = node.Attributes["ReferenceColum"].Value;
                }
                map.Relations.Add(relation.Property, relation);
            }
            configuration.Mappings.Add(map);
            return true;
        }

        private EntityRelation GetManyToManyRelation(XmlNode node)
        {
            if (node.ChildNodes.Count != 2)
            {
                return null;
            }

            ManyToManyRelation relation = new ManyToManyRelation();
            relation.Type = RelationType.MantToMany;
            relation.KeyColum = node.Attributes["KeyColum"].Value;
            relation.Property = node.Attributes["Property"].Value;
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode childNote = node.ChildNodes.Item(i);
                switch (childNote.Name)
                {
                    case "ReferenceClass":
                        relation.ReferenceClassName = childNote.Attributes["Name"].Value;
                        relation.ReferenceClassKeyColum = childNote.Attributes["KeyColum"].Value;
                        break;
                    case "ReferenceTable":
                        relation.ReferenceTableName = childNote.Attributes["Name"].Value;
                        relation.ReferenceColum = childNote.Attributes["ReferenceColum"].Value;
                        relation.ReferenceClassColum = childNote.Attributes["ReferenceClassColum"].Value;
                        break;
                    default:
                        return null;
                }
            }
            return relation;
        }
    }
}