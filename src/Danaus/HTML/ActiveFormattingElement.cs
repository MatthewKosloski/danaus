using Danaus.DOM;

namespace Danaus.HTML;

sealed class ActiveFormattingElement(Element? element)
{
    public Element? Element { get; } = element;

    public bool IsMarker => Element is null;
}