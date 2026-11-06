using System.Xml;
using ORMFramework.Exceptions;

namespace ORMFramework.Configuration;

public sealed partial class ConfigManager
{
    public partial ConfigManager(string? configFilePath)
    {
        ConfigFilePath = string.IsNullOrEmpty(configFilePath) ? Path.Combine(Environment.CurrentDirectory, "Config.xml") : configFilePath;
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
