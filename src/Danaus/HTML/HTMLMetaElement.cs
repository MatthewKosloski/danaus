using Danaus.Core;
using Danaus.DOM;

namespace Danaus.HTML;

// https://html.spec.whatwg.org/multipage/semantics.html#htmlmetaelement
class HTMLMetaElement(Document document) : HTMLElement(document, TagName.Meta)
{

    public Attr? Name => Attributes.GetNamedItemNS(null, "name");
    public Attr? HttpEquiv => Attributes.GetNamedItemNS(null, "http-equiv");
    public Attr? Content => Attributes.GetNamedItemNS(null, "content");
    public Attr? Media => Attributes.GetNamedItemNS(null, "media");

}