using Danaus.DOM;

namespace Danaus.HTML;

sealed class ActiveFormattingElement
{
    public Element? Element { get; }

    public bool IsMarker => Element is null;

    public ActiveFormattingElement(Element element)
    {
        Element = element;
    }
}