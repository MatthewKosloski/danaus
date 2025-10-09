namespace Danaus.SVG;

public sealed class TagName(string name): Core.TagName(name)
{
    public static TagName Desc => new("desc");

    public static TagName Title => new("title");
    public static TagName ForeignObject => new("foreignObject");
}