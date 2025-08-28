using Danaus.DOM;

namespace Danaus.HTML;

// https://html.spec.whatwg.org/multipage/dom.html#htmlelement
class HTMLElement(Document document, TagName tagName): Element(document, tagName.Name)
{
    public HTMLElement(Document document): this(document, TagName.Html)
	{
	}
}