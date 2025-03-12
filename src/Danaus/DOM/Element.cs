using Danaus.Core;
using Danaus.HTML.CustomElements;

namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#concept-element-custom-element-state
enum CustomElementState
{
    Custom,
    Failed,
    Precustomized,
    Uncustomized,
    Undefined,
}

// https://dom.spec.whatwg.org/#interface-element
class Element: Node
{
    public string? NamespaceURI { get; } = null;

    public string? Prefix { get; }

    public string LocalName { get; }

    public NamedNodeMap Attributes { get; }

    public CustomElementDefinition? CustomElementDefinition { get; }

    public string? IsValue { get; set; }

    public CustomElementState CustomElementState { get; set; }

    public Element(Document document, string localName, string? namespace_ = null, string? prefix = null): base(document)
    {
        Attributes = new(this);
        LocalName = localName;
        NamespaceURI = namespace_;
        Prefix = prefix;
    }

    public bool Is(TagName tagName)
    {
        return LocalName == tagName.Name;
    }

    public bool IsOneOf(params TagName[] tagNames)
    {
        return tagNames.Any(t => t.Name == LocalName);
    }

    public bool IsInHTMLNamespace => NamespaceURI is not null && NamespaceURI == Namespace.HTML;

    public bool IsInMathMLNamespace => NamespaceURI is not null && NamespaceURI == Namespace.MathML;

    // https://html.spec.whatwg.org/multipage/parsing.html#mathml-text-integration-point
    public bool IsMathMLTextIntegrationPoint => IsInMathMLNamespace
        && IsOneOf(MathML.TagName.Mi, MathML.TagName.Mo, MathML.TagName.Mn, MathML.TagName.Ms, MathML.TagName.Mtext);

    // TODO
    // https://html.spec.whatwg.org/multipage/parsing.html#html-integration-point
    public bool IsHTMLIntegrationPoint => false;

    // https://dom.spec.whatwg.org/#concept-element-qualified-name
    private string QualifiedName => Prefix is null
        ? LocalName
        : $"{Prefix}:{LocalName}";

    // https://dom.spec.whatwg.org/#concept-node-equals
    public override bool Equals(Object? other)
    {
        if (other == null || other is not Element)
        {
            return false;
        }
        else
        {
            var otherElement = (Element)other;

            var thisAttrs = Attributes.AsList();
            var otherAttrs = otherElement.Attributes.AsList();

            if (thisAttrs.Count != otherAttrs.Count)
            {
                return false;
            }

            var hasSameAttrs = true;

            for (var i = 0; i < thisAttrs.Count; i++)
            {
                var thisAttr = thisAttrs.ElementAt(i);
                var otherAttr = otherAttrs.ElementAt(i);

                if (!thisAttr.Equals(otherAttr))
                {
                    hasSameAttrs = false;
                    break;
                }
            }
            
            return hasSameAttrs 
                && NamespaceURI == otherElement.NamespaceURI
                && Prefix == otherElement.Prefix
                && LocalName == otherElement.LocalName;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            NamespaceURI?.GetHashCode(),
            Prefix?.GetHashCode(),
            LocalName.GetHashCode(),
            Attributes.GetHashCode());
    }
}