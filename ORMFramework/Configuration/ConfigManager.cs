using System.Xml;
using ORMFramework.Exceptions;

namespace ORMFramework.Configuration;
public sealed class ConfigManager(string? configFilePath = null)
{
    public string ConfigFilePath { get; set; } = string.IsNullOrEmpty(configFilePath) ? Path.Combine(Environment.CurrentDirectory, "Config.xml") : configFilePath;

    public Configuration GetSystemConfiguration()
    {
        using var fs = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read);
        using var reader = new XmlTextReader(fs) { WhitespaceHandling = WhitespaceHandling.None };
        if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
            throw new OrmConfigurationException("Invalid config file: missing XML declaration");
        if (!reader.Read() || reader.NodeType != XmlNodeType.Element || reader.Name != "Configuration")
            throw new OrmConfigurationException("Invalid config file: root element is not 'Configuration'");
        var configuration = new Configuration { ConnectionString = string.Empty, ProviderName = string.Empty };
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
        if (!result) throw new OrmConfigurationException("Invalid config file");
        return configuration;
    }

    private static bool ReadSessionFactory(XmlTextReader reader, Configuration configuration)
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

    private static bool ReadConnectionString(XmlTextReader reader, Configuration configuration)
    {
        bool result = false;
        if (reader.Read() && reader.NodeType == XmlNodeType.Text)
        {
            configuration.ConnectionString = reader.ReadString().Trim();
            result = true;
        }
        return result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "ConnectionString";
    }

    private static bool ReadProviderName(XmlTextReader reader, Configuration configuration)
    {
        bool result = false;
        if (reader.Read() && reader.NodeType == XmlNodeType.Text)
        {
            configuration.ProviderName = reader.ReadString().Trim();
            result = true;
        }
        return result && reader.NodeType == XmlNodeType.EndElement && reader.Name == "ProviderName";
    }

    private static bool ReadMappings(XmlTextReader reader, Configuration configuration)
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

    private static bool ReadMap(XmlTextReader reader, Configuration configuration)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(reader.ReadSubtree());
        XmlNode mapNode = xmlDoc.ChildNodes.Item(0) ?? throw new OrmConfigurationException("Invalid config file: empty Map element");
        var map = new EntityMapping
        {
            ClassName = GetRequiredAttribute(mapNode, "ClassName"),
            Keys = GetRequiredAttribute(mapNode, "Key").Split(',', StringSplitOptions.RemoveEmptyEntries),
            TableName = GetRequiredAttribute(mapNode, "TableName"),
        };
        for (int i = 0; i < mapNode.ChildNodes.Count; i++)
        {
            XmlNode node = mapNode.ChildNodes.Item(i) ?? throw new OrmConfigurationException("Invalid config file: empty relation element");
            EntityRelation relation;
            if (node.Name == "Many-to-Many")
            {
                EntityRelation? manyRelation = GetManyToManyRelation(node);
                if (manyRelation == null)
                {
                    return false;
                }
                relation = manyRelation;
            }
            else
            {
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
                relation = new EntityRelation
                {
                    Type = relationType.Value,
                    KeyColum = GetRequiredAttribute(node, "KeyColum"),
                    Property = GetRequiredAttribute(node, "Property"),
                    ReferenceClassName = GetRequiredAttribute(node, "ReferenceClass"),
                    ReferenceColum = GetRequiredAttribute(node, "ReferenceColum"),
                };
            }
            map.Relations.Add(relation.Property, relation);
        }
        configuration.Mappings.Add(map);
        return true;
    }

    private static EntityRelation? GetManyToManyRelation(XmlNode node)
    {
        if (node.ChildNodes.Count != 2)
        {
            return null;
        }

        string? referenceClassName = null;
        string? referenceClassKeyColum = null;
        string? referenceTableName = null;
        string? referenceColum = null;
        string? referenceClassColum = null;
        for (int i = 0; i < node.ChildNodes.Count; i++)
        {
            XmlNode childNote = node.ChildNodes.Item(i) ?? throw new OrmConfigurationException("Invalid config file: empty child element");
            switch (childNote.Name)
            {
                case "ReferenceClass":
                    referenceClassName = GetRequiredAttribute(childNote, "Name");
                    referenceClassKeyColum = GetRequiredAttribute(childNote, "KeyColum");
                    break;
                case "ReferenceTable":
                    referenceTableName = GetRequiredAttribute(childNote, "Name");
                    referenceColum = GetRequiredAttribute(childNote, "ReferenceColum");
                    referenceClassColum = GetRequiredAttribute(childNote, "ReferenceClassColum");
                    break;
                default:
                    return null;
            }
        }
        return new ManyToManyRelation
        {
            KeyColum = GetRequiredAttribute(node, "KeyColum"),
            Property = GetRequiredAttribute(node, "Property"),
            ReferenceClassName = referenceClassName ?? throw new OrmConfigurationException("Invalid config file: missing ReferenceClass element"),
            ReferenceColum = referenceColum ?? throw new OrmConfigurationException("Invalid config file: missing ReferenceTable element"),
            ReferenceTableName = referenceTableName ?? throw new OrmConfigurationException("Invalid config file: missing ReferenceTable element"),
            ReferenceClassKeyColum = referenceClassKeyColum ?? throw new OrmConfigurationException("Invalid config file: missing ReferenceClass element"),
            ReferenceClassColum = referenceClassColum ?? throw new OrmConfigurationException("Invalid config file: missing ReferenceTable element"),
            Type = RelationType.MantToMany,
        };
    }

    private static string GetRequiredAttribute(XmlNode node, string name) =>
        node.Attributes?[name]?.Value ?? throw new OrmConfigurationException($"Element '{node.Name}' is missing required attribute '{name}'.");
}