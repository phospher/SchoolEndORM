using System.Xml;
using ORMFramework.Exceptions;

namespace ORMFramework.Configuration;

public sealed partial class ConfigManager
{
    public partial ConfigManager(string? configFilePath = null);

    public string ConfigFilePath { get; set; } = string.Empty;

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
}
