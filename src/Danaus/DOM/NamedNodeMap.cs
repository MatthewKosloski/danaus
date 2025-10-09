namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#namednodemap
sealed class NamedNodeMap(Element element)
{
    private Element Element { get; } = element;

    private List<Attr> List { get; set; } = [];

    // https://dom.spec.whatwg.org/#dom-namednodemap-length
    public int Length => List.Count;

    // https://dom.spec.whatwg.org/#dom-namednodemap-setnameditem
    public Attr? SetNamedItem(Attr attr)
    {
        return SetAttribute(attr);
    }

    // https://dom.spec.whatwg.org/#dom-namednodemap-getnameditemns
    public Attr? GetNamedItemNS(string? namespace_, string localName)
    {
        return GetAttributeNS(namespace_, localName);
    }

    public bool Contains(string name)
    {
        return List.Any(attr => attr.Name == name);
    }

    // https://dom.spec.whatwg.org/#concept-element-attributes-get-by-namespace
    private Attr? GetAttributeNS(string? namespace_, string localName)
    {
        // 1. If namespace is the empty string, then set it to null.
        if (namespace_ == string.Empty)
        {
            namespace_ = null;
        }

        // 2. Return the attribute in element’s attribute list whose namespace
        //    is namespace and local name is localName, if any; otherwise null.
        Attr? result = null;
    
        foreach (var attr in Element.Attributes.List)
        {
            if (attr.NamespaceURI == namespace_ && attr.LocalName == localName)
            {
                result = attr;
                break;
            }
        }

        return result;
    }

    // https://dom.spec.whatwg.org/#concept-element-attributes-set
    private Attr? SetAttribute(Attr attr)
    {
        // TODO
        // 1. If attr’s element is neither null nor element, throw an "InUseAttributeError" DOMException.

        // 2. Let oldAttr be the result of getting an attribute given
        //    attr’s namespace, attr’s local name, and element.
        var oldAttr = GetAttributeNS(attr.NamespaceURI, attr.LocalName);

        // 3. If oldAttr is attr, return attr.
        if (oldAttr == attr)
        {
            return attr;
        }

        // 4. If oldAttr is non-null, then replace oldAttr with attr.
        if (oldAttr is not null)
        {
            ReplaceAttribute(oldAttr, attr);
        }
        // 5. Otherwise, append attr to element.
        else
        {
            AppendAttribute(attr);
        }

        // 6. Return oldAttr.
        return oldAttr;
    }

    // https://dom.spec.whatwg.org/#concept-element-attributes-replace
    private void ReplaceAttribute(Attr oldAttribute, Attr newAttribute)
    {
        // 1. Let element be oldAttribute’s element.
        var element = oldAttribute.OwnerElement;

        if (element is null)
        {
            throw new InvalidOperationException();
        }

        // 2. Replace oldAttribute by newAttribute in element’s attribute list.
        Element.Attributes.List.RemoveAt(GetIndexOf(oldAttribute));
        Element.Attributes.List.Add(newAttribute);

        // 3. Set newAttribute’s element to element.
        newAttribute.OwnerElement = element;

        // 4. Set newAttribute’s node document to element’s node document.
        newAttribute.Document = element.Document;

        // 5. Set oldAttribute’s element to null.
        oldAttribute.OwnerElement = null;

        // TODO
        // 6. Handle attribute changes for oldAttribute with
        //    element, oldAttribute’s value, and newAttribute’s value.
    }

    // https://dom.spec.whatwg.org/#concept-element-attributes-append
    private void AppendAttribute(Attr attr)
    {
        // 1. Append attribute to element’s attribute list.
        Element.Attributes.List.Add(attr);

        // 2. Set attribute’s element to element.
        attr.OwnerElement = Element;

        // 3. Set attribute’s node document to element’s node document.
        attr.Document = Element.Document;

        // TODO
        // 4. Handle attribute changes for attribute with element, null, and attribute’s value.
    }

    private int GetIndexOf(Attr attribute)
    {
        foreach (var attr in Element.Attributes.List)
        {
            if (attr.NamespaceURI == attribute.NamespaceURI && attr.LocalName == attribute.LocalName)
            {
                return Element.Attributes.List.IndexOf(attr);
            }
        }

        return -1;
    }

    public List<Node> AsList()
    {
        var list = new List<Node>();

        foreach (var node in List)
        {
            list.Add(node);
        }

        return list;
    }
}