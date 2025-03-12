using Danaus.HTML.CustomElements;

namespace Danaus.DOM;

class ElementFactory
{
    // https://dom.spec.whatwg.org/#concept-create-element
    public static Element CreateElement(Document document, string localName, string? namespace_, string? prefix = null, string? is_ = null, bool synchronousCustomElements = false)
    {
        // 1. Let result be null.
        Element? result = null;

        // TODO
        // 2. Let definition be the result of looking up a custom element definition given
        //    document, namespace, localName, and is.
        // CustomElementDefinition? definition = null;

        // TODO
        // 3. If definition is non-null, and definition’s name is not equal to its
        //    local name (i.e., definition represents a customized built-in element):

        // TODO
        // 4. Otherwise, if definition is non-null:

        // 5. Otherwise:

            // 1. Let interface be the element interface for localName and namespace.

            // 2. Set result to a new element that implements interface, with no attributes,
            //    namespace set to namespace, namespace prefix set to prefix, local name set to localName,
            //    custom element state set to "uncustomized", custom element definition set to null,
            //    is value set to is, and node document set to document.
            result = new Element(document, localName, namespace_, prefix)
            {
                CustomElementState = CustomElementState.Uncustomized,
                IsValue = is_
            };

            // TODO
            // 3. If namespace is the HTML namespace, and either localName is a valid custom element name
            //    or is is non-null, then set result’s custom element state to "undefined".

        // 6. Return result.
        return result;
    }
}