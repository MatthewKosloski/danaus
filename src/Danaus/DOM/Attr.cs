namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#attr
sealed class Attr(Document document, string localName, string value = ""): Node(document)
{
    public string? NamespaceURI { get; } = null;
    
    public string? Prefix { get; } = null;
    
    public string LocalName { get; } = localName;
    
    public string Name => QualifiedName;
    
    public string Value { get; } = value;

    public Element? OwnerElement { get; set; } = null;

    // https://dom.spec.whatwg.org/#dom-attr-specified
    public bool Specified => true;

    // https://dom.spec.whatwg.org/#concept-attribute-qualified-name
    private string QualifiedName => Prefix is null
        ? LocalName
        : $"{Prefix}:{LocalName}";

    // https://dom.spec.whatwg.org/#concept-node-equals
    public override bool Equals(Object? other)
    {
        if (other == null || other is not Attr)
        {
            return false;
        }
        else
        {
            var otherAttr = (Attr)other;
            
            return NamespaceURI == otherAttr.NamespaceURI
                && LocalName == otherAttr.LocalName
                && Value == otherAttr.Value;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            NamespaceURI?.GetHashCode(),
            LocalName.GetHashCode(),
            Value.GetHashCode());
    }
}