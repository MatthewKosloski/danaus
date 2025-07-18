using System.Reflection;

namespace Danaus.HTML;

public sealed class TagName(string name) : Core.TagName(name)
{

    private static readonly Dictionary<string, TagName> _tagNames = [];

    static TagName()
    {
        // Initialize the dictionary with all tag names
        var fields = typeof(TagName).GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(TagName))
            {
                var tagName = (TagName)field.GetValue(null)!;
                _tagNames[tagName.Name] = tagName;
            }
        }
    }

    public static TagName? FromString(string name)
    {
        return _tagNames.TryGetValue(name, out var tagName) ? tagName : null;
    }

    public static TagName A => new("a");
    public static TagName Address => new("address");
    public static TagName Applet => new("applet");
    public static TagName Area => new("area");
    public static TagName Article => new("article");
    public static TagName Aside => new("aside");
    public static TagName B => new("b");
    public static TagName Base => new("base");
    public static TagName Basefront => new("basefront");
    public static TagName Bgsound => new("bgsound");
    public static TagName Big => new("big");
    public static TagName Blockquote => new("blockquote");
    public static TagName Body => new("body");
    public static TagName Br => new("br");
    public static TagName Button => new("button");
    public static TagName Caption => new("caption");
    public static TagName Center => new("center");
    public static TagName Code => new("code");
    public static TagName Col => new("col");
    public static TagName Colgroup => new("colgroup");
    public static TagName Dd => new("dd");
    public static TagName Details => new("details");
    public static TagName Dialog => new("dialog");
    public static TagName Dir => new("dir");
    public static TagName Div => new("div");
    public static TagName Dl => new("dl");
    public static TagName Dt => new("dt");
    public static TagName Em => new("em");
    public static TagName Embed => new("embed");
    public static TagName Fieldset => new("fieldset");
    public static TagName Figcaption => new("figcaption");
    public static TagName Figure => new("figure");
    public static TagName Font => new("Font");
    public static TagName Footer => new("footer");
    public static TagName Form => new("form");
    public static TagName Frame => new("frame");
    public static TagName Frameset => new("frameset");
    public static TagName H1 => new("h1");
    public static TagName H2 => new("h2");
    public static TagName H3 => new("h3");
    public static TagName H4 => new("h4");
    public static TagName H5 => new("h5");
    public static TagName H6 => new("h6");
    public static TagName Head => new("head");
    public static TagName Header => new("header");
    public static TagName Hgroup => new("hgroup");
    public static TagName Hr => new("hr");
    public static TagName Html => new("html");
    public static TagName I => new("I");
    public static TagName Iframe => new("iframe");
    public static TagName Image => new("image");
    public static TagName Img => new("img");
    public static TagName Input => new("input");
    public static TagName Keygen => new("keygen");
    public static TagName Li => new("li");
    public static TagName Link => new("link");
    public static TagName Listing => new("listing");
    public static TagName Main => new("main");
    public static TagName Marquee => new("marquee");
    public static TagName Math => new("math");
    public static TagName Menu => new("menu");
    public static TagName Meta => new("meta");
    public static TagName Nav => new("nav");
    public static TagName Nobr => new("nobr");
    public static TagName Noembed => new("noembed");
    public static TagName Noframes => new("noframes");
    public static TagName Noscript => new("noscript");
    public static TagName Object => new("object");
    public static TagName Ol => new("ol");
    public static TagName Optgroup => new("optgroup");
    public static TagName Option => new("option");
    public static TagName P => new("p");
    public static TagName Param => new("param");
    public static TagName Plaintext => new("plaintext");
    public static TagName Pre => new("pre");
    public static TagName Rb => new("rb");
    public static TagName Rp => new("rp");
    public static TagName Rt => new("rt");
    public static TagName Rtc => new("rtc");
    public static TagName S => new("S");
    public static TagName Sarcasm => new("sarcasm");
    public static TagName Script => new("script");
    public static TagName Search => new("search");
    public static TagName Section => new("section");
    public static TagName Select => new("select");
    public static TagName Small => new("Small");
    public static TagName Source => new("source");
    public static TagName Strike => new("Strike");
    public static TagName Strong => new("Strong");
    public static TagName Style => new("style");
    public static TagName Summary => new("summary");
    public static TagName Svg => new("svg");
    public static TagName Table => new("table");
    public static TagName Tbody => new("tbody");
    public static TagName Td => new("td");
    public static TagName Template => new("template");
    public static TagName Textarea => new("textarea");
    public static TagName Tfoot => new("tfoot");
    public static TagName Th => new("th");
    public static TagName Thead => new("thead");
    public static TagName Title => new("title");
    public static TagName Tr => new("tr");
    public static TagName Track => new("track");
    public static TagName Tt => new("Tt");
    public static TagName U => new("U");
    public static TagName Ul => new("ul");
    public static TagName Wbr => new("wbr");
    public static TagName Xmp => new("xmp");
}