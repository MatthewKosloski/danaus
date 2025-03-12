using System.Diagnostics;
using Danaus.Core;
using Danaus.DOM;
using Danaus.HTML.CustomElements;

namespace Danaus.HTML;

// https://html.spec.whatwg.org/multipage/parsing.html#the-insertion-mode
enum InsertionMode
{
    AfterAfterBody,
    AfterAfterFrameset,
    AfterBody,
    AfterFrameset,
    AfterHead,
    BeforeHead,
    BeforeHTML,
    InBody,
    InCaption,
    InCell,
    InColumnGroup,
    InFrameset,
    InHead,
    InHeadNoScript,
    Initial,
    InRow,
    InSelect,
    InSelectInTable,
    InTable,
    InTableBody,
    InTableText,
    InTemplate,
    Text,
}

class AdjustedInsertionLocation
{
    public Node? Parent { get; set; } = null;
    public Node? Before { get; set; } = null;
    public Node? After { get; set; } = null;
}

// https://html.spec.whatwg.org/multipage/parsing.html#tree-construction
class HTMLParser(HTMLTokenizer tokenizer)
{
    private HTMLTokenizer Tokenizer { get; } = tokenizer;

    private Document Document { get; } = new Document();

    private InsertionMode InsertionMode { get; } = InsertionMode.Initial;

    private InsertionMode? OriginalInsertionMode { get; } = null;

    private Stack<InsertionMode> TemplateInsertionModes { get; } = new();

    private InsertionMode? CurrentTemplateInsertionMode { get; } = null;

    private Queue<Element> StackOfOpenElements { get; } = new();

    private bool IsFosterParentingEnabled { get; } = false;

    private Element? CurrentNode => StackOfOpenElements.LastOrDefault();

    // TODO: https://html.spec.whatwg.org/multipage/parsing.html#adjusted-current-node
    public Element? AdjustedCurrentNode => CurrentNode;

    private Element? Context { get; } = null;

    public void Run()
    {
        while (true)
        {
            var token = Tokenizer.NextToken();

            if (token is not null)
            {

                // TODO: https://html.spec.whatwg.org/multipage/parsing.html#tree-construction-dispatcher
                if (false
                    // If the stack of open elements is empty 
                    || StackOfOpenElements.Count == 0
                    // If the adjusted current node is an element in the HTML namespace
                    || (AdjustedCurrentNode is not null && AdjustedCurrentNode.IsInHTMLNamespace)
                    // If the adjusted current node is a MathML text integration point and the token is a start tag whose tag name is neither "mglyph" nor "malignmark"
                    || (AdjustedCurrentNode is not null && AdjustedCurrentNode.IsMathMLTextIntegrationPoint && token.IsStartTag() && !token.IsOneOfTags(MathML.TagName.Mglypth, MathML.TagName.Malignmark))
                    // If the adjusted current node is a MathML text integration point and the token is a character token
                    || (AdjustedCurrentNode is not null && AdjustedCurrentNode.IsMathMLTextIntegrationPoint && token.IsCharacterToken())
                    // If the adjusted current node is a MathML annotation-xml element and the token is a start tag whose tag name is "svg"
                    || (AdjustedCurrentNode is not null && AdjustedCurrentNode.IsInMathMLNamespace && AdjustedCurrentNode.Is(MathML.TagName.Annotation_xml) && token.IsStartTag(TagName.Svg))
                    // If the adjusted current node is an HTML integration point and the token is a start tag
                    || (AdjustedCurrentNode is not null && AdjustedCurrentNode.IsHTMLIntegrationPoint && token.IsStartTag())
                    // If the adjusted current node is an HTML integration point and the token is a character token
                    || (AdjustedCurrentNode is not null && AdjustedCurrentNode.IsHTMLIntegrationPoint && token.IsCharacterToken())
                    // If the token is an end-of-file token
                    || token.IsEndOfFileToken())
                {
                    // Process the token according to the rules given in the section corresponding
                    // to the current insertion mode in HTML content.
                    ProcessToken(token);
                }
            }
        }
    }

    private void ProcessToken(HTMLToken token)
    {
        switch (InsertionMode)
        {
            case InsertionMode.Initial:
            {

                var document = new Document();

                if (token.IsWhiteSpaceCharacter())
                {
                    // Ignore the token.
                }
                else if (token.IsCommentToken())
                {
                    InsertComment((CommentToken)token);
                }

                break;
            }
            default:
            {
                throw new UnreachableException($"Unhandled insertion mode {InsertionMode}");
                
            }
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-comment
    private void InsertComment(CommentToken token)
    {
        // 1. Let data be the data given in the comment token being processed.
        var data = token.Data;

        // 2. If position was specified, then let the adjusted insertion location be position.
        //    Otherwise, let adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = GetAppropriatePlaceForInsertingANode();

        // 3. Create a Comment node whose data attribute is set to data and
        //    whose node document is the same as that of the node in which the
        //    adjusted insertion location finds itself.

        // 4. Insert the newly created node at the adjusted insertion location.
        
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#creating-and-inserting-nodes
    private AdjustedInsertionLocation GetAppropriatePlaceForInsertingANode(Element? overrideTarget = null)
    {
        AdjustedInsertionLocation result = new();

        // 1. If there was an override target specified, then let target be the override target.
        //    Otherwise, let target be the current node.
        Element? target = overrideTarget is not null
            ? overrideTarget
            : CurrentNode;

        // 2. Determine the adjusted insertion location using the first matching steps from the following list:
    
        // If foster parenting is enabled and target is a table, tbody, tfoot, thead, or tr element
        if (IsFosterParentingEnabled && target is not null && target.IsOneOf(TagName.Table, TagName.Tbody, TagName.Tfoot, TagName.Thead, TagName.Tr))
        {
            // TODO
        }
        // Otherwise, let adjusted insertion location be inside target, after its last child (if any).
        else if (target is not null)
        {
            result = new AdjustedInsertionLocation
            {
                Parent = target,
                After = target.LastChild,
            };
        }

        // TODO
        // 3. If the adjusted insertion location is inside a template element,
        //    let it instead be inside the template element's template contents, after its last child (if any).

        // 4. Return the adjusted insertion location.
        return result;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#create-an-element-for-the-token
    private Element? CreateElementFor(TagToken token, string namespace_, Element intendedParent)
    {
        // TODO
        // 1. If the active speculative HTML parser is not null, then
        //    return the result of creating a speculative mock element given
        //    given namespace, the tag name of the given token, and 
        //    the attributes of the given token.

        // 2. Otherwise, optionally create a speculative mock element given given namespace,
        //    the tag name of the given token, and the attributes of the given token.

        // 3. Let document be intended parent's node document.
        var document = intendedParent.Document;

        if (document is null)
        {
            return null;
        }

        // 4. Let local name be the tag name of the token.
        var localName = token.Name;

        // 5. Let is be the value of the "is" attribute in the given token,
        //    if such an attribute exists, or null otherwise.
        string? is_ = token.Attributes.GetValueOrDefault("is");

        // TODO
        // 6. Let definition be the result of looking up a custom element definition
        //    given document, given namespace, local name, and is.
        // CustomElementDefinition? definition = null;

        // TODO
        // 7. Let willExecuteScript be true if definition is non-null and
        //    the parser was not created as part of the HTML fragment parsing algorithm;
        //    otherwise false.
        var willExecuteScript = false;

        // TODO
        // 8. If willExecuteScript is true:
        //   1. Increment document's throw-on-dynamic-markup-insertion counter.
        //   2. If the JavaScript execution context stack is empty, then perform a microtask checkpoint.
        //   3. Push a new element queue onto document's relevant agent's custom element reactions stack.

        // 9. Let element be the result of creating an element given document, localName,
        //    given namespace, null, is, and willExecuteScript.
        var element = ElementFactory.CreateElement(document, localName, namespace_, null, is_, willExecuteScript);

        // 10. Append each attribute in the given token to element.
        foreach (var kvp in token.Attributes)
        {
            element.Attributes.SetNamedItem(new Attr(document, kvp.Key, kvp.Value));
        }

        // TODO
        // 11. If willExecuteScript is true:
            // 1. Let queue be the result of popping from document's relevant agent's custom element reactions stack.
            //    (This will be the same element queue as was pushed above.)
            // 2. Invoke custom element reactions in queue.
            // 3. Decrement document's throw-on-dynamic-markup-insertion counter.

        // TODO
        // 12. If element has an xmlns attribute in the XMLNS namespace whose value is not exactly the same as
        //     the element's namespace, that is a parse error. Similarly, if element has an xmlns:xlink attribute
        //     in the XMLNS namespace whose value is not the XLink Namespace, that is a parse error.

        // TODO
        // 13. If element is a resettable element, invoke its reset algorithm.
        //     (This initializes the element's value and checkedness based on the element's attributes.)

        // TODO
        // 14. If element is a form-associated element and not a form-associated custom element,
        //     the form element pointer is not null, there is no template element on the stack of open elements,
        //     element is either not listed or doesn't have a form attribute, and the intended parent is
        //     in the same tree as the element pointed to by the form element pointer, then associate element with
        //     the form element pointed to by the form element pointer and set element's parser inserted flag.

        // 15. Return element.
        return element;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-element-at-the-adjusted-insertion-location
    private void InsertElementAtTheAdjustedInsertionLocation(Element element)
    {
        // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = GetAppropriatePlaceForInsertingANode();

        // 2. If it is not possible to insert element at the adjusted insertion location, abort these steps.
        if (adjustedInsertionLocation.Parent is null)
        {
            return;
        }

        // 3. If the parser was not created as part of the HTML fragment parsing algorithm,
        //    then push a new element queue onto element's relevant agent's custom element reactions stack.

        // 4. Insert element at the adjusted insertion location.

        // 5. If the parser was not created as part of the HTML fragment parsing algorithm,
        //    then pop the element queue from element's relevant agent's custom element reactions stack,
        //    and invoke custom element reactions in that queue.

    }

}