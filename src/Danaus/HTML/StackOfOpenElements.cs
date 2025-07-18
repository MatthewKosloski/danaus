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

    public void Clear()
    {
        List.Clear();
    }

    public bool ContainsOneOf(params TagName[] tagNames)
    {
        foreach (var element in List)
        {
            if (tagNames.Any(t => t.Name == element.LocalName))
            {
                return true;
            }
        }

        return false;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-the-specific-scope
    public bool HasElementInSpecificScope(Core.TagName target, params Core.TagName[] tagNames)
    {
        // 1. Initialize node to be the current node (the bottommost node of the stack).
        var i = 1;
        var node = List[^i];

        while (node is not null)
        {
            // 2. If node is target node, terminate in a match state.
            if (node.Is(target))
                return true;

            // 3. Otherwise, if node is one of the element types in list, terminate in a failure state.
            if (tagNames.Any(t => node.Is(t)))
                return false;

            // 4. Otherwise, set node to the previous entry in the stack of open elements and return to step 2.
            i++;
            node = List[^i];
        }

        return false;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-scope
    public bool HasElementInScope(Core.TagName target)
    {
        return HasElementInSpecificScope(target,
            TagName.Applet, TagName.Caption, TagName.Html, TagName.Table, TagName.Td,
            TagName.Th, TagName.Marquee, TagName.Object, TagName.Template,
            MathML.TagName.Mi, MathML.TagName.Mo, MathML.TagName.Mn, MathML.TagName.Ms,
            MathML.TagName.Mtext, MathML.TagName.Annotation_xml,
            SVG.TagName.ForeignObject, SVG.TagName.Desc, SVG.TagName.Title
        );
    }

    public bool HasElementInScopeWithTagName(HTMLToken token)
    {

        return HasElementInScope(((TagToken)token).TagName);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-button-scope
    public bool HasElementInButtonScope(Core.TagName target)
    {
        return HasElementInSpecificScope(target,
            TagName.Applet, TagName.Caption, TagName.Html, TagName.Table, TagName.Td,
            TagName.Th, TagName.Marquee, TagName.Object, TagName.Template,
            MathML.TagName.Mi, MathML.TagName.Mo, MathML.TagName.Mn, MathML.TagName.Ms,
            MathML.TagName.Mtext, MathML.TagName.Annotation_xml,
            SVG.TagName.ForeignObject, SVG.TagName.Desc, SVG.TagName.Title,
            TagName.Button
        );
    }

    public Element? PopUntil(TagName tagName)
    {
        Element? popped;
        do
        {
            popped = Pop();
        } while (popped is not null && popped.LocalName != tagName);

        return popped;
    }

    public Element? At(int index)
    {
        if (index < 0 || index >= List.Count)
            return null;
        
        return List[index];
    }
}