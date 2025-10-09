namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#documentfragment
class DocumentFragment(Document document, Element? host): Node(document)
{
    // https://dom.spec.whatwg.org/#concept-documentfragment-host
    public Element? Host { get; set; } = host;
}