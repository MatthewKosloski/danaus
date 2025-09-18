namespace Danaus.HTML;

enum TagTokenType {
    Start,
    End,
}

class TagToken(TagTokenType type, string name, bool isSelfClosing = false, Dictionary<string, string>? attributes = null): HTMLToken 
{
    public TagTokenType Type { get; } = type;
    public string Name { get; protected set; } = name;
    public bool IsSelfClosing { get; set; } = isSelfClosing;
    public Dictionary<string, string> Attributes { get; } = attributes ?? [];

    public bool SelfClosingAcknowledged { get; protected set; } = false;

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
            Attributes[name] = value + c;
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

    public void ClearAttributes()
    {
        Attributes.Clear();
    }

    public void AcknowledgeSelfClosingFlagIfSet()
    {
        if (IsSelfClosing)
        {
            SelfClosingAcknowledged = true;
        }
    }

    public bool Matches(HTMLToken token)
    {
        return token is TagToken tagToken && tagToken.Name == Name;
    }

    public TagName TagName
    {
        get
        {
            var tagName = TagName.FromString(Name);

            if (tagName is not null)
            {
                return tagName;
            }
            else
            {
                throw new InvalidOperationException($"TagToken does not have a valid tag name: {Name}");
            }
        }
    }
}