using System.Xml;

namespace ORMFramework.Configuration;
public sealed class ConfigManager(string? configFilePath = null)
{
    public string ConfigFilePath { get; set; } = string.IsNullOrEmpty(configFilePath) ? Path.Combine(Environment.CurrentDirectory, "Config.xml") : configFilePath;

    public Configuration GetSystemConfiguration()
    {
        using var fs = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read);
        using var reader = new XmlTextReader(fs) { WhitespaceHandling = WhitespaceHandling.None };
        if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
            throw new Exception("Invail Config File");
        if (!reader.Read() || reader.NodeType != XmlNodeType.Element || reader.Name != "Configuration")
            throw new Exception("Invail Config File");
        var configuration = new Configuration();
        var result = true;
        while (result && reader.Read() && reader.NodeType == XmlNodeType.Element)
        {
            result = reader.Name switch
            {
                "SessionFactory" => ReadSessionFactory(reader, configuration),
                "Mappings" => ReadMappings(reader, configuration),
                _ => result,
            };
        }
        if (!result) throw new Exception("Invail Config File");
        return configuration;
    }

    private bool ReadSessionFactory(XmlTextReader reader, Configuration configuration)
    {
        bool result = true;
        while (result && reader.Read() && reader.NodeType == XmlNodeType.Element)
        {
            switch (reader.Name)
            {
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
        return result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "SessionFactory";
    }

    private bool ReadConnectionString(XmlTextReader reader, Configuration configuration)
    {
        bool result = false;
        if (reader.Read() && reader.NodeType == XmlNodeType.Text)
        {
            configuration.ConnectionString = reader.ReadString().Trim();
            result = true;
        }
        return result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "ConnectionString";
    }

    private bool ReadProviderName(XmlTextReader reader, Configuration configuration)
    {
        bool result = false;
        if (reader.Read() && reader.NodeType == XmlNodeType.Text)
        {
            configuration.ProviderName = reader.ReadString().Trim();
            result = true;
        }
        return result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "ProviderName";
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
        var xmlDoc = new XmlDocument();
        var map = new EntityMapping();
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
                RelationType? relationType = node.Name switch
                {
                    "One-to-Many" => RelationType.OneToMany,
                    "One-to-One" => RelationType.OneToOne,
                    "Many-to-One" => RelationType.ManyToOne,
                    _ => null,
                };
                if (relationType is null)
                {
                    return false;
                }
                relation.Type = relationType.Value;
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

        var relation = new ManyToManyRelation();
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