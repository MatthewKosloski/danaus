using System.Diagnostics;
using Danaus.Core;
using Danaus.DOM;

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
    public Node? Target { get; set; } = null;
    public Node? Before { get; set; } = null;
}

// https://html.spec.whatwg.org/multipage/parsing.html#tree-construction
class HTMLParser(Document document, HTMLTokenizer tokenizer)
{
    private HTMLTokenizer Tokenizer = tokenizer;

    private Document Document = document;

    private InsertionMode InsertionMode = InsertionMode.Initial;

    private InsertionMode? OriginalInsertionMode;

    private Stack<InsertionMode> TemplateInsertionModes = new();

    private InsertionMode? CurrentTemplateInsertionMode = null;

    private Stack<Element> StackOfOpenElements = new();

    private bool IsFosterParentingEnabled = false;

    private Element? CurrentNode => StackOfOpenElements.LastOrDefault();

    // TODO: https://html.spec.whatwg.org/multipage/parsing.html#adjusted-current-node
    private Element? AdjustedCurrentNode => CurrentNode;

    private Element? Context;

    private HTMLToken? CurrentToken;

    private readonly Queue<HTMLToken> TokenBuffer = new();
    
    private bool ShouldReprocess = false;

    // https://html.spec.whatwg.org/multipage/parsing.html#head-element-pointer
    private Element? HeadElement = null;

    public void Run()
    {
        while (true)
        {
            var token = NextToken();

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
            // https://html.spec.whatwg.org/multipage/parsing.html#the-initial-insertion-mode
            case InsertionMode.Initial:
            {
                if (token.IsWhiteSpaceCharacter())
                {
                    // Ignore the token.
                }
                else if (token.IsCommentToken())
                {
                    InsertComment((CommentToken)token);
                }
                else if (token.IsDocTypeToken())
                {
                    var doctypeToken = (DocTypeToken)token;
                    var doctypeNode = new DocumentType(doctypeToken.Name ?? string.Empty, document)
                    {
                        PublicID = doctypeToken.PublicIdentifier ?? string.Empty,
                        SystemID = doctypeToken.SystemIdentifier ?? string.Empty,
                    };
                    Document.InsertBefore(doctypeNode);

                    SwitchTo(InsertionMode.BeforeHTML);
                }
                else
                {
                    ReprocessIn(InsertionMode.BeforeHTML);
                }

                break;
            }
            // https://html.spec.whatwg.org/multipage/parsing.html#the-before-html-insertion-mode
            case InsertionMode.BeforeHTML:
            {
                if (token.IsDocTypeToken())
                {
                    // Parse error. Ignore the token.
                }
                else if (token.IsCommentToken())
                {
                    // Insert a comment as the last child of the Document object.

                    var commentToken = (CommentToken)token;
                    var insertionLocation = new AdjustedInsertionLocation
                    {
                        Target = Document,
                    };
                    InsertComment(commentToken, insertionLocation);
                }
                else if (token.IsWhiteSpaceCharacter())
                {
                    // Ignore the token.
                }
                else if (token.IsStartTag(TagName.Html))
                {
                    // Create an element for the token in the HTML namespace,
                    // with the Document as the intended parent.
                    var el = CreateElementFor((TagToken)token, Namespace.HTML, Document);

                    if (el is not null)
                    {
                        // Append it to the Document object.
                        Document.InsertBefore(el);
                        
                        // Put this element in the stack of open elements.
                        StackOfOpenElements.Push(el);
                    }
                }
                else if (token.IsOneOfEndTags(TagName.Head, TagName.Body, TagName.Html, TagName.Br))
                {
                    // Create an html element whose node document is the Document object.
                    var el = new HTMLElement(Document);

                    // Append it to the Document object.
                    Document.InsertBefore(el);

                    // Put this element in the stack of open elements.
                    StackOfOpenElements.Push(el);

                    // Switch the insertion mode to "before head", then reprocess the token.
                    ReprocessIn(InsertionMode.BeforeHead);
                }
                else if (token.IsEndTag())
                {
                    // Parse error. Ignore the token.
                }
                else
                {
                    // Create an html element whose node document is the Document object.
                    var el = new HTMLElement(Document);

                    // Append it to the Document object.
                    Document.InsertBefore(el);

                    // Put this element in the stack of open elements.
                    StackOfOpenElements.Push(el);

                    // Switch the insertion mode to "before head", then reprocess the token.
                    ReprocessIn(InsertionMode.BeforeHead);
                }
                break;
            }
            // https://html.spec.whatwg.org/multipage/parsing.html#the-before-head-insertion-mode
            case InsertionMode.BeforeHead:
            {
                if (token.IsWhiteSpaceCharacter())
                {
                    // Ignore the token.
                }
                else if (token.IsCommentToken())
                {
                    // Insert a comment.
                    InsertComment((CommentToken)token);
                }
                else if (token.IsDocTypeToken())
                {
                    // Parse error. Ignore the token.
                }
                else if (token.IsStartTag(TagName.Html))
                {
                    // Process the token using the rules for the "in body" insertion mode.
                    ReprocessIn(InsertionMode.InBody);
                }
                else if (token.IsStartTag(TagName.Head))
                {
                    // Insert an HTML element for the token.
                    var headElement = InsertHTMLElementFor((TagToken)token);

                    // Set the head element pointer to the newly created head element.
                    HeadElement = headElement;

                    // Switch the insertion mode to "in head".
                    SwitchTo(InsertionMode.InHead);
                }
                else if (token.IsOneOfEndTags(TagName.Head, TagName.Body, TagName.Html, TagName.Br))
                {
                    // Insert an HTML element for a "head" start tag token with no attributes.
                    var headToken = new TagToken(TagTokenType.Start, "head");
                    var headElement = InsertHTMLElementFor(headToken);

                    // Set the head element pointer to the newly created head element.
                    HeadElement = headElement;

                    // Switch the insertion mode to "in head".
                    // Reprocess the current token.
                    ReprocessIn(InsertionMode.InHead);
                }
                else if (token.IsEndTag())
                {
                    // Parse error. Ignore the token.
                }
                else
                {
                    // Insert an HTML element for a "head" start tag token with no attributes.
                    var headToken = new TagToken(TagTokenType.Start, "head");
                    var headElement = InsertHTMLElementFor(headToken);

                    // Set the head element pointer to the newly created head element.
                    HeadElement = headElement;

                    // Switch the insertion mode to "in head".
                    // Reprocess the current token.
                    ReprocessIn(InsertionMode.InHead);
                }
                break;
            }
            // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inhead
            case InsertionMode.InHead:
            {
                if (token.IsWhiteSpaceCharacter())
                {
                    InsertCharacter((CharacterToken)token);
                }
                else if (token.IsCommentToken())
                {
                    InsertComment((CommentToken)token);
                }
                else if (token.IsDocTypeToken())
                {
                    // Parse error. Ignore the token.
                }
                else if (token.IsStartTag(TagName.Html))
                {
                    // Process the token using the rules for the "in body" insertion mode.
                    ReprocessIn(InsertionMode.InBody);
                }
                else if (token.IsOneOfStartTags(TagName.Base, TagName.Basefront, TagName.Bgsound, TagName.Link))
                {
                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // Immediately pop the current node off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // TODO - Acknowledge the token's self-closing flag, if it is set.
                }
                else if (token.IsStartTag(TagName.Meta))
                {
                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // Immediately pop the current node off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // TODO - Acknowledge the token's self-closing flag, if it is set.

                    // TODO - If the active speculative HTML parser is null, then:
                    // TODO - 1. If the element has a charset attribute, and getting an encoding from its value
                    //    results in an encoding, and the confidence is currently tentative,
                    //    then change the encoding to the resulting encoding.
                    // TODO - 2. Otherwise, if the element has an http-equiv attribute whose value is
                    //     an ASCII case-insensitive match for the string "Content-Type",
                    //     and the element has a content attribute, and applying the algorithm for extracting a
                    //     character encoding from a meta element to that attribute's value returns an encoding,
                    //     and the confidence is currently tentative, then change the encoding to the extracted encoding.
                }
                else if (token.IsStartTag(TagName.Title))
                {
                    ParseGenericRCDATAElement((TagToken)token);
                }
                else if ((token.IsStartTag(TagName.Noscript) && Document.IsScriptingEnabled)
                    || token.IsOneOfStartTags(TagName.Noframes, TagName.Style))
                {
                    ParseGenericRawTextElement((TagToken)token);
                }
                else if (token.IsStartTag(TagName.Noscript) && !Document.IsScriptingEnabled)
                {
                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // Switch the insertion mode to "in head noscript".
                    SwitchTo(InsertionMode.InHeadNoScript);
                }
                else if (token.IsStartTag(TagName.Script))
                {
                    // TODO
                }
                else if (token.IsEndTag(TagName.Head))
                {
                    // Pop the current node (which will be the head element) off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // Switch the insertion mode to "after head".
                    SwitchTo(InsertionMode.AfterHead);
                }
                else if (token.IsOneOfEndTags(TagName.Body, TagName.Html, TagName.Br))
                {
                    // Pop the current node off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // Switch the insertion mode to "after head".
                    // Reprocess the token.
                    ReprocessIn(InsertionMode.AfterHead);
                }
                else if (token.IsStartTag(TagName.Template))
                {
                    // TODO
                }
                else if (token.IsEndTag(TagName.Template))
                {
                    // TODO
                }
                else if (token.IsStartTag(TagName.Head) || token.IsEndTag())
                {
                    // Parse error. Ignore the token.
                }
                else
                {
                    // Pop the current node off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // Switch the insertion mode to "after head".
                    // Reprocess the token.
                    ReprocessIn(InsertionMode.AfterHead);  
                }
                break;
            }
            // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inheadnoscript
            case InsertionMode.InHeadNoScript:
            {
                break;
            }
            default:
            {
                throw new UnreachableException($"Unhandled insertion mode {InsertionMode}");
            }
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#generic-rcdata-element-parsing-algorithm
    private void ParseGenericRCDATAElement(TagToken token)
    {
        // 1. Insert an HTML element for the token.
        InsertHTMLElementFor(token);

        // 2. Switch the tokenizer to the RCDATA state.
        Tokenizer.SwitchTo(State.RCDATA);

        // 3. Let the original insertion mode be the current insertion mode.
        OriginalInsertionMode = InsertionMode;

        // 4. Then, switch the insertion mode to "text".
        SwitchTo(InsertionMode.Text);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#generic-raw-text-element-parsing-algorithm
    private void ParseGenericRawTextElement(TagToken token)
    {
        // 1. Insert an HTML element for the token.
        InsertHTMLElementFor(token);

        // 2. Switch the tokenizer to the RAWTEXT state.
        Tokenizer.SwitchTo(State.RAWTEXT);

        // 3. Let the original insertion mode be the current insertion mode.
        OriginalInsertionMode = InsertionMode;

        // 4. Then, switch the insertion mode to "text".
        SwitchTo(InsertionMode.Text);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-comment
    private void InsertComment(CommentToken token, AdjustedInsertionLocation? position = null)
    {
        // 1. Let data be the data given in the comment token being processed.
        var data = token.Data;

        // 2. If position was specified, then let the adjusted insertion location be position.
        //    Otherwise, let adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = position is not null
            ? position
            : GetAppropriatePlaceForInsertingANode();

        // 3. Create a Comment node whose data attribute is set to data and
        //    whose node document is the same as that of the node in which the
        //    adjusted insertion location finds itself.
        var comment = new Comment(document, data);

        // 4. Insert the newly created node at the adjusted insertion location.
        if (adjustedInsertionLocation.Target is not null)
        {
            adjustedInsertionLocation.Target.InsertBefore(comment, adjustedInsertionLocation.Before);
        }
        
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-character
    private void InsertCharacter(CharacterToken token)
    {
        // 1. Let data be the characters passed to the algorithm, or, if no characters were explicitly specified,
        //    the character of the character token being processed.
        var data = token.Data;

        // 2. Let the adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = GetAppropriatePlaceForInsertingANode();

        // 3. If the adjusted insertion location is in a Document node, then return.
        //    Note: The DOM will not let Document nodes have Text node children, so they are dropped on the floor.
        if (adjustedInsertionLocation.Target is Document)
        {
            return;
        }

        // 4. If there is a Text node immediately before the adjusted insertion location,
        //    then append data to that Text node's data.
        //    Otherwise, create a new Text node whose data is data and whose node document
        //    is the same as that of the element in which the adjusted insertion location finds itself,
        //    and insert the newly created node at the adjusted insertion location.

        if (adjustedInsertionLocation?.Before?.PreviousSibling is Text)
        {
            ((Text)adjustedInsertionLocation.Before.PreviousSibling).AppendData(data.ToString());
        }
        else if (adjustedInsertionLocation?.Target?.LastChild is Text)
        {
            ((Text)adjustedInsertionLocation.Target.LastChild).AppendData(data.ToString());
        }
        else if (adjustedInsertionLocation?.Target is not null)
        {
            var text = new Text(adjustedInsertionLocation.Target.Document, data.ToString());
            adjustedInsertionLocation.Target.InsertBefore(text, adjustedInsertionLocation.Before);
        }
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
        if (IsFosterParentingEnabled
            && target is not null
            && target.IsOneOf(TagName.Table, TagName.Tbody, TagName.Tfoot, TagName.Thead, TagName.Tr))
        {
            // TODO
        }
        // Otherwise, let adjusted insertion location be inside target, after its last child (if any).
        else if (target is not null)
        {
            result = new AdjustedInsertionLocation
            {
                Target = target,
                Before = null,
            };
        }

        // TODO
        // 3. If the adjusted insertion location is inside a template element,
        //    let it instead be inside the template element's template contents, after its last child (if any).

        // 4. Return the adjusted insertion location.
        return result;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#create-an-element-for-the-token
    private Element? CreateElementFor(TagToken token, Namespace namespace_, Node intendedParent)
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
        var element = ElementFactory.CreateElement(document, localName, namespace_.Name, null, is_, willExecuteScript);

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

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-html-element
    private Element? InsertHTMLElementFor(TagToken token)
    {
        return InsertForeignElement(token, Namespace.HTML, false);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-foreign-element
    private Element? InsertForeignElement(TagToken token, Namespace _namespace, bool onlyAddToElementStack)
    {
        // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = GetAppropriatePlaceForInsertingANode();

        if (adjustedInsertionLocation.Target is null)
        {
            return null;
        }

        // 2. Let element be the result of creating an element for the token in the given namespace,
        //    with the intended parent being the element in which the adjusted insertion location finds itself.
        var element = CreateElementFor(token, _namespace, adjustedInsertionLocation.Target);

        if (element is null)
        {
            return null;
        }

        // 3. If onlyAddToElementStack is false, then run insert an element at the adjusted insertion location with element.
        if (!onlyAddToElementStack)
        {
            InsertElementAtTheAdjustedInsertionLocation(element);
        }

        // 4. Push element onto the stack of open elements so that it is the new current node.
        StackOfOpenElements.Push(element);

        // 5. Return element.
        return element;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-element-at-the-adjusted-insertion-location
    private void InsertElementAtTheAdjustedInsertionLocation(Element element)
    {
        // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = GetAppropriatePlaceForInsertingANode();

        // 2. If it is not possible to insert element at the adjusted insertion location, abort these steps.
        if (adjustedInsertionLocation.Target is null)
        {
            return;
        }

        // 3. TODO - If the parser was not created as part of the HTML fragment parsing algorithm,
        //    then push a new element queue onto element's relevant agent's custom element reactions stack.

        // 4. Insert element at the adjusted insertion location.
        adjustedInsertionLocation.Target.InsertBefore(element, adjustedInsertionLocation.Before);

        // 5. If the parser was not created as part of the HTML fragment parsing algorithm,
        //    then pop the element queue from element's relevant agent's custom element reactions stack,
        //    and invoke custom element reactions in that queue.

        // 6. If the parser was not created as part of the HTML fragment parsing algorithm, 
        //    then pop the element queue from element's relevant agent's custom element reactions stack, 
        //    and invoke custom element reactions in that queue.
    }

    private HTMLToken? NextToken()
    {
        if (!ShouldReprocess)
        {
            // Read from our internal buffer before checking the tokenizer.
            var next = TokenBuffer.Count > 0
                ? TokenBuffer.Dequeue()
                : Tokenizer.NextToken();

            CurrentToken = next;
        }
        else
        {
            ShouldReprocess = false;
        }

        return CurrentToken;
    }

    private void SwitchTo(InsertionMode mode)
    {
        InsertionMode = mode;
    }

    private void ReprocessIn(InsertionMode mode)
    {
        ShouldReprocess = true;
        SwitchTo(mode);
    }
}