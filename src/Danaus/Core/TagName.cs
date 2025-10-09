namespace Danaus.Core;

public abstract class TagName(string name)
{

    public string Name { get; } = name;

    public static bool operator ==(TagName left, string right)
    {
        return left.Name == right;
    }

    public static bool operator ==(string left, TagName right)
    {
        return left == right.Name;
    }

    public static bool operator !=(TagName left, string right)
    {
        return left.Name != right;
    }

    public static bool operator !=(string left, TagName right)
    {
        return left != right.Name;
    }

    public override bool Equals(Object? other)
    {
        if (other == null || other is not TagName)
        {
            return false;
        }
        else
        {
            var otherTagName = (TagName)other;
            return otherTagName.Name == Name;
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