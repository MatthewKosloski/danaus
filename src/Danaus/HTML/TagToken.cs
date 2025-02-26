namespace Danaus.HTML;

enum TagTokenType {
    Start,
    End,
}

class TagToken(TagTokenType type, string name, bool isSelfClosing = false): HTMLToken 
{
    public TagTokenType Type { get; } = type;
    public string Name { get; protected set; } = name;
    public bool IsSelfClosing { get; } = isSelfClosing;
    public Dictionary<string, string> Attributes { get; } = [];

    public void AppendToName(char c)
    {
        Name += c;
    }

    public void AppendToAttributeName(string name, char c)
    {
        bool hasAttribute = Attributes.TryGetValue(name, out string? value);

        if (!hasAttribute)
        {
            throw new InvalidOperationException($"TagToken does not have an attribute ${name}");
        }

        if (value is not null)
        {
            Attributes.Remove(name);
            Attributes.Add(name + c, value);
        }
    }

    public void AppendToAttributeValue(string name, char c)
    {
        bool hasAttribute = Attributes.TryGetValue(name, out string? value);

        if (!hasAttribute)
        {
            throw new InvalidOperationException($"TagToken does not have an attribute ${name}");
        }

        if (value is not null)
        {
            Attributes.Add(name, value + c);
        }
    }

    public bool IsStart()
    {
        return Type == TagTokenType.Start;
    }

    public bool IsEnd()
    {
        return Type == TagTokenType.End;
    }

    public bool Matches(HTMLToken token)
    {
        return token is TagToken tagToken && tagToken.Name == Name;
    }
}