namespace Danaus.HTML;

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

    public bool IsEmpty()
    {
        return elements.Count == 0;
    }
}