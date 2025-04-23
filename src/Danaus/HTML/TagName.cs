namespace Danaus.HTML;

public sealed class TagName(string name): Core.TagName(name)
{
    public static TagName Base => new("base");
    public static TagName Basefront => new("basefront");
    public static TagName Bgsound => new("bgsound");
    public static TagName Body => new("body");
    public static TagName Br => new("br");
    public static TagName Frameset => new("frameset");
    public static TagName Head => new("head");
    public static TagName Html => new("html");
    public static TagName Link => new("link");
    public static TagName Meta => new("meta");
    public static TagName Noframes => new("noframes");
    public static TagName Noscript => new("noscript");
    public static TagName Script => new("script");
    public static TagName Style => new("style");
    public static TagName Svg => new("svg");
    public static TagName Table => new("table");
    public static TagName Tbody => new("tbody");
    public static TagName Template => new("template");
    public static TagName Tfoot => new("tfoot");
    public static TagName Thead => new("thead");
    public static TagName Title => new("title");
    public static TagName Tr => new("tr");
}