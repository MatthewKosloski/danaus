namespace Danaus.HTML;

public sealed class TagName(string name): Core.TagName(name)
{
    public static TagName Svg => new("svg");
    public static TagName Table => new("table");
    public static TagName Tbody => new("tbody");
    public static TagName Tfoot => new("tfoot");
    public static TagName Thead => new("thead");
    public static TagName Tr => new("tr");
}