namespace Danaus.Core;

// https://infra.spec.whatwg.org/#namespaces
public sealed class Namespace
{

    public string Name { get; }

    public static Namespace HTML => new("http://www.w3.org/1999/xhtml");

    public static Namespace MathML => new("http://www.w3.org/1998/Math/MathML");

    public static Namespace SVG => new("http://www.w3.org/2000/svg");

    public static Namespace XLink => new("http://www.w3.org/1999/xlink");

    public static Namespace XML => new("http://www.w3.org/XML/1998/namespace");
    
    public static Namespace XMLNS => new("http://www.w3.org/2000/xmlns/");

    private Namespace(string name)
    {
        Name = name;
    }

    public static bool operator ==(Namespace left, string right)
    {
        return left.Name == right;
    }

    public static bool operator ==(string left, Namespace right)
    {
        return left == right.Name;
    }

    public static bool operator !=(Namespace left, string right)
    {
        return left.Name != right;
    }

    public static bool operator !=(string left, Namespace right)
    {
        return left != right.Name;
    }

    public override bool Equals(Object? other)
    {
        if (other == null || other is not Namespace)
        {
            return false;
        }
        else
        {
            var otherNamespace = (Namespace)other;
            return otherNamespace.Name == Name;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name.GetHashCode());
    }

    public override string ToString()
    {
        return Name;
    }
}