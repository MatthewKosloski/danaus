namespace Danaus.MathML;

public sealed class TagName(string name): Core.TagName(name)
{
    public static TagName Annotation_xml => new("annotation-xml");
    public static TagName Malignmark => new("malignmark");
    public static TagName Mglypth => new("mglypth");
    public static TagName Mi => new("mi");
    public static TagName Mn => new("mn");
    public static TagName Mo => new("mo");
    public static TagName Ms => new("ms");
    public static TagName Mtext => new("mtext");
}