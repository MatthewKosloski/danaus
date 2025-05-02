using Danaus.Core;
using Danaus.DOM;

namespace Danaus.HTML;

class StackOfOpenElements
{
    private List<Element> List { get; } = [];

    public bool IsEmpty => List.Count == 0;

    public int Count => List.Count;

    public Element? CurrentElement
    {
        get
        {
            if (List.Count == 0)
                return null;
            return List[^1];
        }
    }

    public void Push(Element element)
    {
        List.Add(element);
    }

    public Element Pop()
    {
        if (List.Count == 0)
            throw new InvalidOperationException("Stack is empty.");

        var element = List[^1];
        List.RemoveAt(List.Count - 1);
        return element;
    }

    public bool ContainsTemplateElement()
    {
        foreach (var element in List)
        {
            if (element.NamespaceURI is not null && element.NamespaceURI != Namespace.HTML)
                continue;
            if (element.LocalName == TagName.Template)
                return true;
        }

        return false;
    }

    public Element? At(int index)
    {
        if (index < 0 || index >= List.Count)
            return null;
        
        return List[index];
    }
}