using Danaus.DOM;

namespace Danaus.HTML;

// https://html.spec.whatwg.org/multipage/parsing.html#list-of-active-formatting-elements
class ListOfActiveFormattingElements
{
    private readonly List<ActiveFormattingElement> elements = [];

    public void Add(ActiveFormattingElement element)
    {
        elements.Add(element);
    }

    public void Remove(ActiveFormattingElement element)
    {
        elements.Remove(element);
    }

    public Element? LastElementWithTagNameBeforeMarker(Core.TagName tagName)
    {
        for (var i = elements.Count - 1; i >= 0; i--)
        {
            var element = elements[i];

            // If we encounter a marker, we stop looking.
            if (element.IsMarker)
            {
                break;
            }

            if (element.Element?.Is(tagName) == true)
            {
                return element.Element;
            }
        }
        return null;
    }

    public bool IsEmpty()
    {
        return elements.Count == 0;
    }
}