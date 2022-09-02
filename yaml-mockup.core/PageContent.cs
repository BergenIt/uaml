using YamlDotNet.Serialization;

namespace YamlMockup.Core;

public struct PageContent
{
    public ItemContent[] ItemContents { get; set; }
    public string PageName { get; set; }
    public string PageComment { get; set; }
    public ushort? MinX { get; set; }
    public ushort? MinY { get; set; }
    public bool Debug { get; set; }

    public static PageContent Parse(string uaml)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new UnionJsonConverter())
            .IgnoreFields()
            .Build();

        PageContent pageContent = deserializer.Deserialize<PageContent>(uaml);

        return pageContent;
    }
}
