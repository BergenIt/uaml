namespace YamlMockup.Core;

public class ItemContent
{
    public ItemContentType Type { get; set; }
    
    public string? Name { get; set; }
    public string? Link { get; set; }

    public string? Text { get; set; }

    public bool AllignLeft { get; set; }
    public bool HideLink { get; set; }

    public ushort? FontSize { get; set; }
    public ushort? Border { get; set; }
    public string? Content { get; set; }

    public Union<short, string> X { get; set; } = new(0);
    public Union<short, string> Y { get; set; } = new(0);

    internal short Height { get; set; }
    internal short Width { get; set; }
}
