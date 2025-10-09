using Danaus.WebIDL;

namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#document
class Document: Node
{

    public bool IsScriptingEnabled { get; } = false;

    public Document(): base()
    {
        // The node document of a document is that document itself.
        Document = this;
    }

    // https://dom.spec.whatwg.org/#dom-document-adoptnode
    public Node? AdoptNode(Node node)
    {
        // 1. If node is a document, then throw a "NotSupportedError" DOMException.
        if (node is Document)
        {
            throw new DOMException("Cannot adopt a document into a document", "NotSupportedError");
        }

        // TODO - 2. If node is a shadow root, then throw a "HierarchyRequestError" DOMException.

        // 3. If node is a DocumentFragment node whose host is non-null, then return.
        if (node is DocumentFragment fragment && fragment.Host is not null)
        {
            return null;
        }

        // 4. Adopt node into this.
        Adopt(node);

        // 5. Return node.
        return node;
    }

    // https://dom.spec.whatwg.org/#concept-node-adopt
    private void Adopt(Node node)
    {
        // 1. Let oldDocument be node’s node document.
        var oldDocument = node.Document;

        // 2. If node’s parent is non-null, then remove node.
        if (node.ParentNode is not null)
        {
            node.Remove();
        }

        // 3. TODO - If document is not oldDocument:
            // 1. TODO - For each inclusiveDescendant in node’s shadow-including inclusive descendants:
                // 1. TODO - Set inclusiveDescendant’s node document to document.
                // 2. TODO - If inclusiveDescendant is an element, then set the node document of each attribute in inclusiveDescendant’s attribute list to document.
            // 2. TODO - For each inclusiveDescendant in node’s shadow-including inclusive descendants that is custom, enqueue a custom element callback reaction with inclusiveDescendant, callback name "adoptedCallback", and « oldDocument, document ».
            // 3. TODO - For each inclusiveDescendant in node’s shadow-including inclusive descendants, in shadow-including tree order, run the adopting steps with inclusiveDescendant and oldDocument.
    }

    // https://dom.spec.whatwg.org/#dom-document-createnodeiterator
    public NodeIterator CreateNodeIterator(Node root, ulong whatToShow = 0xFFFFFFFF, NodeFilter? filter = null)
    {
        // 1. Let iterator be a new NodeIterator object.
        // 2. Set iterator’s root and iterator’s reference to root.
        // 3. Set iterator’s pointer before reference to true.
        // 4. Set iterator’s whatToShow to whatToShow.
        // 5. Set iterator’s filter to filter.
        var iterator = new NodeIterator(root, root)
        {
            PointerBeforeReferenceNode = true,
            WhatToShow = whatToShow,
            Filter = filter,
        };

        // 6. Return iterator.
        return iterator;
    }

}