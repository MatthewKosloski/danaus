namespace Danaus.HTML;

enum TagTokenType {
    Start,
    End,
}

class TagToken(TagTokenType type, string name, bool isSelfClosing = false): HTMLToken 
{
    public TagTokenType Type { get; } = type;
    public string Name { get; } = name;
    public bool IsSelfClosing { get; } = isSelfClosing;
    public Dictionary<string, string>? Attributes { get; } = [];
}