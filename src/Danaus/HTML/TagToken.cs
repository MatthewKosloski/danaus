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
    public Dictionary<string, string>? Attributes { get; } = [];

    public void AppendToName(char c)
    {
        Name += c;
    }
}