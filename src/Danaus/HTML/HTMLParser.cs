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

    private StackOfOpenElements StackOfOpenElements = new();

    private bool IsFosterParentingEnabled = false;

    private Element? CurrentNode => StackOfOpenElements.CurrentElement;

    // TODO: https://html.spec.whatwg.org/multipage/parsing.html#adjusted-current-node
    private Element? AdjustedCurrentNode => CurrentNode;

    private Element? Context;

    private HTMLToken? CurrentToken;

    private readonly Queue<HTMLToken> TokenBuffer = new();
    
    private bool ShouldReprocess = false;

    // https://html.spec.whatwg.org/multipage/parsing.html#head-element-pointer
    private Element? HeadElement = null;

    // https://html.spec.whatwg.org/multipage/parsing.html#frameset-ok-flag
    private bool FramesetOK = true;

    // https://html.spec.whatwg.org/multipage/parsing.html#list-of-active-formatting-elements
    private ListOfActiveFormattingElements ActiveFormattingElements = new();

    // https://html.spec.whatwg.org/multipage/parsing.html#stack-of-template-insertion-modes
    private Stack<InsertionMode> StackOfTemplateInsertionModes = new();

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
                    || StackOfOpenElements.IsEmpty
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
                if (token.IsDocTypeToken())
                {
                    // Parse error. Ignore the token.
                }
                else if (token.IsStartTag(TagName.Html))
                {
                    // Process the token using the rules for the "in body" insertion mode.
                    ReprocessIn(InsertionMode.InBody);
                }
                else if (token.IsEndTag(TagName.Noscript))
                {
                    // Pop the current node (which will be a noscript element) from the stack of open elements;
                    // the new current node will be a head element.
                    StackOfOpenElements.Pop();

                    // Switch the insertion mode to "in head".
                    SwitchTo(InsertionMode.InHead);
                }
                else if (token.IsWhiteSpaceCharacter()
                    || token.IsCommentToken()
                    || token.IsOneOfStartTags(
                        TagName.Basefront, TagName.Bgsound, TagName.Link,
                        TagName.Meta, TagName.Noframes, TagName.Style))
                {
                    // Process the token using the rules for the "in head" insertion mode.
                    ReprocessIn(InsertionMode.InHead);
                }
                else if (token.IsEndTag(TagName.Br))
                {
                    // Parse error.

                    // Pop the current node (which will be a noscript element) from the stack of open elements;
                    // the new current node will be a head element.
                    StackOfOpenElements.Pop();

                    // Switch the insertion mode to "in head".
                    // Reprocess the token.
                    ReprocessIn(InsertionMode.InHead);
                }
                else if (token.IsOneOfStartTags(TagName.Head, TagName.Noscript) || token.IsEndTag())
                {
                    // Parse error. Ignore the token.
                }
                else
                {
                    // Parse error.

                    // Pop the current node (which will be a noscript element) from the stack of open elements;
                    // the new current node will be a head element.
                    StackOfOpenElements.Pop();

                    // Switch the insertion mode to "in head".
                    // Reprocess the token.
                    ReprocessIn(InsertionMode.InHead);
                }
                break;
            }
            // https://html.spec.whatwg.org/multipage/parsing.html#the-after-head-insertion-mode
            case InsertionMode.AfterHead:
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
                else if (token.IsStartTag(TagName.Body))
                {
                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // Set the frameset-ok flag to "not ok".
                    FramesetOK = false;

                    // Switch the insertion mode to "in body".
                    SwitchTo(InsertionMode.InBody);
                }
                else if (token.IsStartTag(TagName.Frameset))
                {
                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // Switch the insertion mode to "in frameset".
                    SwitchTo(InsertionMode.InFrameset);
                }
                else if (token.IsOneOfStartTags(
                    TagName.Base, TagName.Basefront, TagName.Bgsound, TagName.Link,
                    TagName.Meta, TagName.Noframes, TagName.Script, TagName.Style,
                    TagName.Template, TagName.Title))
                {
                    // Parse error.
                    
                    if (HeadElement is not null)
                    {
                        // Push the node pointed to by the head element pointer onto the stack of open elements.
                        StackOfOpenElements.Push(HeadElement);

                        // Process the token using the rules for the "in head" insertion mode.
                        ReprocessIn(InsertionMode.InHead);

                        // Remove the node pointed to by the head element pointer from the stack of open elements.
                        StackOfOpenElements.Pop();
                    }
                }
                else if (token.IsEndTag(TagName.Template))
                {
                    // Process the token using the rules for the "in head" insertion mode.
                    ReprocessIn(InsertionMode.InHead);
                }
                else if (token.IsOneOfEndTags(TagName.Body, TagName.Html, TagName.Br))
                {
                    // Insert an HTML element for a "body" start tag token with no attributes.
                    InsertHTMLElementFor(new TagToken(TagTokenType.Start, TagName.Body.Name));

                    // Switch the insertion mode to "in body".
                    // Reprocess the current token.
                    ReprocessIn(InsertionMode.InBody);
                }
                else if (token.IsStartTag(TagName.Head) || token.IsEndTag())
                {
                    // Parse error. Ignore the token.
                }
                else
                {
                    // Insert an HTML element for a "body" start tag token with no attributes.
                    InsertHTMLElementFor(new TagToken(TagTokenType.Start, TagName.Body.Name));

                    // Switch the insertion mode to "in body".
                    // Reprocess the current token.
                    ReprocessIn(InsertionMode.InBody);
                }

                break;
            }
            // https://html.spec.whatwg.org/multipage/parsing.html#the-in-body-insertion-mode
            case InsertionMode.InBody:
            {
                if (token.IsCodePoint(CodePoint.NullCharacter))
                {
                    // Parse error. Ignore the token.   
                }
                else if (token.IsWhiteSpaceCharacter())
                {
                    // Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // Insert the token's character.
                    InsertCharacter((CharacterToken)token);
                }
                else if (token.IsCharacterToken())
                {
                    // Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // Insert the token's character.
                    InsertCharacter((CharacterToken)token);

                    // Set the frameset-ok flag to "not ok".
                    FramesetOK = false;
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
                    // Parse error.

                    // If there is a template element on the stack of open elements, then ignore the token.
                    if (StackOfOpenElements.ContainsOneOf(TagName.Template))
                    {
                        // Ignore the token.
                    }
                    else
                    {
                        // Otherwise, for each attribute on the token, check to see if
                        // the attribute is already present on the top element of the stack of open elements.
                        // If it is not, add the attribute and its corresponding value to that element.
                        var topElement = StackOfOpenElements.CurrentElement;
                        var tok = token as TagToken;

                        if (topElement is not null && tok is not null)
                        {
                            foreach (var kvp in tok.Attributes)
                            {
                                if (!topElement.Attributes.Contains(kvp.Key))
                                {
                                    topElement.Attributes.SetNamedItem(new Attr(document, kvp.Key, kvp.Value));
                                }
                            }
                        }
                    }
                }
                else if (token.IsOneOfStartTags(TagName.Base, TagName.Basefront, TagName.Bgsound,
                TagName.Link, TagName.Meta, TagName.Noframes, TagName.Script, TagName.Style,
                TagName.Template, TagName.Title) || token.IsEndTag(TagName.Template))
                {
                    // Process the token using the rules for the "in head" insertion mode.
                    ReprocessIn(InsertionMode.InHead);
                }
                else if (token.IsStartTag(TagName.Body))
                {
                    // Parse error.

                    var secondElement = StackOfOpenElements.At(1);

                    // If the stack of open elements has only one node on it,
                    // if the second element on the stack of open elements is not a body element,
                    // or if there is a template element on the stack of open elements,
                    // then ignore the token. (fragment case or there is a template element on the stack)
                    if (StackOfOpenElements.Count == 1
                        || secondElement is not null && secondElement.LocalName != TagName.Body
                        || StackOfOpenElements.ContainsOneOf(TagName.Template))
                    {
                        // Ignore the token.
                    }
                    // Otherwise, set the frameset-ok flag to "not ok"; then, for each attribute on the token,
                    // check to see if the attribute is already present on the body element (the second element)
                    // on the stack of open elements, and if it is not, add the attribute and its corresponding
                    // value to that element.
                    else
                    {
                        FramesetOK = false;

                        var tok = token as TagToken;
                        if (secondElement is not null && tok is not null)
                        {
                            foreach (var kvp in tok.Attributes)
                            {
                                if (!secondElement.Attributes.Contains(kvp.Key))
                                {
                                    secondElement.Attributes.SetNamedItem(new Attr(document, kvp.Key, kvp.Value));
                                }
                            }
                        }
                    }
                }
                else if (token.IsStartTag(TagName.Frameset))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsEndOfFileToken())
                {
                    // If the stack of template insertion modes is not empty,
                    // then process the token using the rules for the "in template" insertion mode.
                    if (StackOfTemplateInsertionModes.Count > 0)
                    {
                        ReprocessIn(InsertionMode.InTemplate);
                    }
                    else
                    {
                        // If there is a node in the stack of open elements that is not either a dd element, 
                        // a dt element, an li element, an optgroup element, an option element, a p element, 
                        // an rb element, an rp element, an rt element, an rtc element, a tbody element, 
                        // a td element, a tfoot element, a th element, a thead element, a tr element, 
                        // the body element, or the html element, then this is a parse error.
                        if (!StackOfOpenElements.ContainsOneOf(
                            TagName.Dd, TagName.Dt, TagName.Li, TagName.Optgroup,
                            TagName.Option, TagName.P, TagName.Rb, TagName.Rp,
                            TagName.Rt, TagName.Rtc, TagName.Tbody, TagName.Td,
                            TagName.Tfoot, TagName.Th, TagName.Thead, TagName.Tr,
                            TagName.Body, TagName.Html))
                        {
                            // Parse error.
                        }

                        // Stop parsing.
                        StopParsing();
                    }
                }
                else if (token.IsEndTag(TagName.Body))
                {
                    // If the stack of open elements does not have a body element in scope, 
                    // this is a parse error; ignore the token.
                    if (!StackOfOpenElements.HasElementInScope(TagName.Body))
                    {
                        // Parse error. Ignore the token.
                    }
                    else if (!StackOfOpenElements.ContainsOneOf(
                        TagName.Dd, TagName.Dt, TagName.Li, TagName.Optgroup,
                        TagName.Option, TagName.P, TagName.Rb, TagName.Rp,
                        TagName.Rt, TagName.Rtc, TagName.Tbody, TagName.Td,
                        TagName.Tfoot, TagName.Th, TagName.Thead, TagName.Tr,
                        TagName.Body, TagName.Html))
                    {
                        // Parse error.        
                    }

                    // Switch the insertion mode to "after body".
                    SwitchTo(InsertionMode.AfterBody);
                }
                else if (token.IsEndTag(TagName.Html))
                {
                    // If the stack of open elements does not have a body element in scope, 
                    // this is a parse error; ignore the token.
                    if (!StackOfOpenElements.HasElementInScope(TagName.Body))
                    {
                        // Parse error. Ignore the token.
                    }
                    else if (!StackOfOpenElements.ContainsOneOf(
                        TagName.Dd, TagName.Dt, TagName.Li, TagName.Optgroup,
                        TagName.Option, TagName.P, TagName.Rb, TagName.Rp,
                        TagName.Rt, TagName.Rtc, TagName.Tbody, TagName.Td,
                        TagName.Tfoot, TagName.Th, TagName.Thead, TagName.Tr,
                        TagName.Body, TagName.Html))
                    {
                        // Parse error.
                    }

                    // Switch the insertion mode to "after body".
                    // Reprocess the token.
                    ReprocessIn(InsertionMode.AfterBody);
                }
                else if (token.IsOneOfStartTags(TagName.Address, TagName.Article, TagName.Aside,
                    TagName.Blockquote, TagName.Center, TagName.Details, TagName.Dialog,
                    TagName.Dir, TagName.Div, TagName.Dl, TagName.Fieldset, TagName.Figcaption,
                    TagName.Figure, TagName.Footer, TagName.Header, TagName.Hgroup, TagName.Main,
                    TagName.Menu, TagName.Nav, TagName.Ol, TagName.P, TagName.Search,
                    TagName.Section, TagName.Summary, TagName.Ul))
                {
                    // If the stack of open elements has a p element in button scope, then close a p element.
                    if (StackOfOpenElements.HasElementInButtonScope(TagName.P))
                    {
                        CloseAPElement();
                    }

                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);
                }
                else if (token.IsOneOfStartTags(TagName.H1, TagName.H2, TagName.H3,
                    TagName.H4, TagName.H5, TagName.H6))
                {
                    // If the stack of open elements has a p element in button scope, then close a p element.
                    if (StackOfOpenElements.HasElementInButtonScope(TagName.P))
                    {
                        CloseAPElement();
                    }

                    // If the current node is an HTML element whose tag name is one of "h1", "h2", "h3", "h4", "h5", or "h6",
                    // then this is a parse error; pop the current node off the stack of open elements.
                    if (CurrentNode is not null && CurrentNode.IsOneOf(TagName.H1, TagName.H2, TagName.H3, TagName.H4, TagName.H5, TagName.H6))
                    {
                        // Parse error.
                        StackOfOpenElements.Pop();
                    }

                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);
                }
                else if (token.IsOneOfStartTags(TagName.Pre, TagName.Listing))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Form))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Li))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Dd, TagName.Dt))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Plaintext))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Button))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfEndTags(TagName.Address, TagName.Article, TagName.Aside,
                    TagName.Blockquote, TagName.Button, TagName.Center, TagName.Details, TagName.Dialog,
                    TagName.Dir, TagName.Div, TagName.Dl, TagName.Fieldset, TagName.Figcaption,
                    TagName.Figure, TagName.Footer, TagName.Header, TagName.Hgroup, TagName.Listing,
                    TagName.Main, TagName.Menu, TagName.Nav, TagName.Ol, TagName.Pre, TagName.Search,
                    TagName.Section, TagName.Summary, TagName.Ul))
                {
                    // If the stack of open elements does not have an element in scope that
                    // is an HTML element with the same tag name as that of the token, then
                    // this is a parse error; ignore the token.
                    if (!StackOfOpenElements.HasElementInScopeWithTagName(token))
                    {
                        // Parse error. Ignore the token.
                    }
                    else
                    {
                        // Otherwise, run these steps:

                        var tagToken = (TagToken)token;

                        // 1. Generate implied end tags.
                        GenerateImpliedEndTags();

                        // 2. If the current node is not an HTML element with the same tag name as that of the token,
                        //    then this is a parse error.
                        if (CurrentNode is not null && !CurrentNode.Is(tagToken.TagName))
                        {
                            // Parse error.
                        }

                        // 3. Pop elements from the stack of open elements until an HTML element with
                        //    the same tag name as the token has been popped from the stack.
                        StackOfOpenElements.PopUntil(tagToken.TagName);
                    }

                }
                else if (token.IsEndTag(TagName.Form))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsEndTag(TagName.P))
                {
                    // If the stack of open elements does not have a p element in button scope,
                    // then this is a parse error; insert an HTML element for a "p" start tag token with no attributes.
                    if (!StackOfOpenElements.HasElementInButtonScope(TagName.P))
                    {
                        // Parse error.
                        InsertHTMLElementFor(new TagToken(TagTokenType.Start, TagName.P.Name));
                    }

                    // Close a p element.
                    CloseAPElement();
                }
                else if (token.IsStartTag(TagName.Li))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Dd, TagName.Dt))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfEndTags(TagName.H1, TagName.H2, TagName.H3,
                    TagName.H4, TagName.H5, TagName.H6))
                {
                    // If the stack of open elements does not have an element in scope that is an HTML element
                    // and whose tag name is one of "h1", "h2", "h3", "h4", "h5", or "h6",
                    // then this is a parse error; ignore the token.
                    if (!StackOfOpenElements.HasElementInScopeWithTagName(token))
                    {
                        // Parse error. Ignore the token.
                    }
                    else
                    {
                        // Otherwise, run these steps:

                        var tagToken = (TagToken)token;

                        // 1. Generate implied end tags.
                        GenerateImpliedEndTags();

                        // 2. If the current node is not an HTML element with the same tag name as that of the token,
                        //    then this is a parse error.
                        if (CurrentNode is not null && !CurrentNode.Is(tagToken.TagName))
                        {
                            // Parse error.
                        }

                        // 3. Pop elements from the stack of open elements until an HTML element whose
                        // tag name is one of "h1", "h2", "h3", "h4", "h5", or "h6" has been popped from the stack.
                        StackOfOpenElements.PopUntil(tagToken.TagName);
                    }
                }
                else if (token.IsEndTag(TagName.Sarcasm))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.A))
                {
                    /*
                    * 1. If the list of active formatting elements contains an a element between
                    *    the end of the list and the last marker on the list (or the start of the
                    *    list if there is no marker on the list), then this is a parse error;
                    *    run the adoption agency algorithm for the token, then remove that element
                    *    from the list of active formatting elements and the stack of open elements
                    *    if the adoption agency algorithm didn't already remove it (it might not have
                    *    if the element is not in table scope).
                    */
                    // TODO

                    // 2. Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // 3. Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // 4. Push the element onto the list of active formatting elements.
                    ActiveFormattingElements.Add(new ActiveFormattingElement(CurrentNode));
                }
                else if (token.IsOneOfStartTags(TagName.B, TagName.Big, TagName.Code,
                    TagName.Em, TagName.Font, TagName.I, TagName.S, TagName.Small, TagName.Strike,
                    TagName.Strong, TagName.Tt, TagName.U))
                {
                    // Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);

                    // Push the element onto the list of active formatting elements.
                    ActiveFormattingElements.Add(new ActiveFormattingElement(CurrentNode));
                }
                else if (token.IsStartTag(TagName.Nobr))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfEndTags(TagName.A, TagName.B, TagName.Big, TagName.Code,
                    TagName.Em, TagName.Font, TagName.I, TagName.Nobr, TagName.S, TagName.Small,
                    TagName.Strike, TagName.Strong, TagName.Tt, TagName.U))
                {
                    // Do this
                    // Run the adoption agency algorithm for the token.
                }
                else if (token.IsOneOfStartTags(TagName.Applet, TagName.Marquee, TagName.Object))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Table))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Br))
                {
                    // Parse error.

                    // Drop the attributes from the token, and act as described in the next entry;
                    // i.e. act as if this was a "br" start tag token with no attributes, rather than
                    // the end tag token that it actually is.
                    var tagToken = (TagToken)token;
                    tagToken.ClearAttributes();

                    // Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // Insert an HTML element for the token.
                    InsertHTMLElementFor(tagToken);

                    // Immediately pop the current node off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // Acknowledge the token's self-closing flag, if it is set.
                    tagToken.AcknowledgeSelfClosingFlagIfSet();

                    // Set the frameset-ok flag to "not ok".
                    FramesetOK = false;

                }
                else if (token.IsOneOfStartTags(TagName.Area, TagName.Br, TagName.Embed, TagName.Img, TagName.Keygen, TagName.Wbr))
                {
                    var tagToken = (TagToken)token;

                    // Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // Insert an HTML element for the token.
                    InsertHTMLElementFor(tagToken);

                    // Immediately pop the current node off the stack of open elements.
                    StackOfOpenElements.Pop();

                    // Acknowledge the token's self-closing flag, if it is set.
                    tagToken.AcknowledgeSelfClosingFlagIfSet();

                    // Set the frameset-ok flag to "not ok".
                    FramesetOK = false;
                }
                else if (token.IsStartTag(TagName.Input))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Param, TagName.Source, TagName.Track))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Hr))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Image))
                {
                    // Parse error. Change the token's tag name to "img" and reprocess it. (Don't ask.)
                    var tokenImg = (TagToken)token;
                    CurrentToken = new TagToken(TagTokenType.Start, TagName.Img.Name, tokenImg.IsSelfClosing, tokenImg.Attributes);
                    ReprocessIn(InsertionMode.InBody);
                }
                else if (token.IsStartTag(TagName.Textarea))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Xmp))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Iframe))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Noembed)
                    || token.IsStartTag(TagName.Noscript) && Document.IsScriptingEnabled)
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Select))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Optgroup, TagName.Option))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Rb, TagName.Rtc))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Rp, TagName.Rt))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Math))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag(TagName.Svg))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsOneOfStartTags(TagName.Caption, TagName.Col, TagName.Colgroup,
                    TagName.Frame, TagName.Head, TagName.Tbody, TagName.Td, TagName.Tfoot,
                    TagName.Th, TagName.Thead, TagName.Tr))
                {
                    // TODO
                    throw new NotImplementedException("Not implemented yet.");
                }
                else if (token.IsStartTag())
                {
                    // Reconstruct the active formatting elements, if any.
                    ReconstructActiveFormattingElements();

                    // Insert an HTML element for the token.
                    InsertHTMLElementFor((TagToken)token);
                }
                else if (token.IsEndTag())
                {
                    var tagToken = (TagToken)token;

                    // 1. Initialize node to be the current node (the bottommost node of the stack).
                    Element? node = null;

                    // 2. Loop: If node is an HTML element with the same tag name as the token, then:
                    for (int i = StackOfOpenElements.Count - 1; i >= 0; i--)
                    {
                        node = StackOfOpenElements.At(i);

                        if (node is not null && node.Is(tagToken.TagName))
                        {
                            // 1. Generate implied end tags, except for HTML elements
                            //    with the same tag name as the token.
                            GenerateImpliedEndTags(tagToken.TagName);

                            // 2. If node is not the current node, then this is a parse error.
                            if (!node.Equals(CurrentNode))
                            {
                                // Parse error.
                            }

                            // 3. Pop all the nodes from the current node up to node, including node,
                            //    then stop these steps.
                            StackOfOpenElements.PopUntil(tagToken.TagName);
                            break;
                        }

                        // 3. Otherwise, if node is in the special category, then this is a parse error;
                        //    ignore the token, and return.
                        if (node is not null && IsSpecial(node))
                        {
                            return;
                        }

                        // 4. Set node to the previous entry in the stack of open elements.
                        // 5. Return to the step labeled loop.
                    }
                }
                break;
            }
            case InsertionMode.Text:
            {
                // Do this next
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InTable:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InTableText:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InCaption:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InColumnGroup:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InTableBody:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InRow:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InCell:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InSelect:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InSelectInTable:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InTemplate:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.AfterBody:
            {
                // Do this next
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.InFrameset:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.AfterFrameset:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.AfterAfterBody:
            {
                // Do this next
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            case InsertionMode.AfterAfterFrameset:
            {
                // TODO
                throw new NotImplementedException("Not implemented yet.");
            }
            default:
            {
                throw new UnreachableException($"Unhandled insertion mode {InsertionMode}");
            }
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#close-a-p-element
    public void CloseAPElement()
    {
        // 1. Generate implied end tags, except for p elements.
        GenerateImpliedEndTags(TagName.P);

        // 2. If the current node is not a p element, then this is a parse error.
        if (CurrentNode is not null && CurrentNode.LocalName != TagName.P)
        {
            // Parse error.
        }

        // 3. Pop elements from the stack of open elements until a p element has been popped from the stack.
        StackOfOpenElements.PopUntil(TagName.P);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#generate-implied-end-tags
    public void GenerateImpliedEndTags(TagName? exclude = null)
    {
        // While the current node is a dd element, a dt element, an li element, an optgroup element,
        // an option element, a p element, an rb element, an rp element, an rt element, or an rtc element,
        // the UA must pop the current node off the stack of open elements.

        var currentNode = StackOfOpenElements.CurrentElement;

        var includeAll = exclude is null;

        while (currentNode is not null)
        {
            if (currentNode.IsOneOf(
                TagName.Dd, TagName.Dt, TagName.Li, TagName.Optgroup,
                TagName.Option, TagName.P, TagName.Rb, TagName.Rp,
                TagName.Rt, TagName.Rtc))
            {
                var shouldExclude = exclude is not null && currentNode.LocalName == exclude;
                if (includeAll || !shouldExclude)
                {
                    StackOfOpenElements.Pop();
                }
            }
            currentNode = StackOfOpenElements.CurrentElement;
        }

    }

    // https://html.spec.whatwg.org/multipage/parsing.html#stop-parsing
    private void StopParsing()
    {
        // TODO - Stop parsing the document.

        // 4. Pop all the nodes off the stack of open elements.
        StackOfOpenElements.Clear();
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#reconstruct-the-active-formatting-elements
    private void ReconstructActiveFormattingElements()
    {
        // 1. If there are no entries in the list of active formatting elements, then
        //    there is nothing to reconstruct; stop this algorithm.

        if (ActiveFormattingElements.IsEmpty())
        {
            return;
        }

        // TODO
        // 2. If the last (most recently added) entry in the list of active formatting elements is a marker,
        //    or if it is an element that is in the stack of open elements, then there is nothing to reconstruct;
        //    stop this algorithm.
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

    // https://html.spec.whatwg.org/multipage/parsing.html#special
    private bool IsSpecial(Element node)
    {
        return node is HTMLElement && (
            node.IsOneOf(
                TagName.Address, TagName.Applet, TagName.Area, TagName.Article,
                TagName.Aside, TagName.Base, TagName.Basefront, TagName.Bgsound,
                TagName.Blockquote, TagName.Body, TagName.Br, TagName.Button,
                TagName.Caption, TagName.Center, TagName.Col, TagName.Colgroup,
                TagName.Dd, TagName.Details, TagName.Dir, TagName.Div, TagName.Dl,
                TagName.Dt, TagName.Embed, TagName.Fieldset, TagName.Figcaption,
                TagName.Figure, TagName.Footer, TagName.Form, TagName.Frame,
                TagName.Frameset, TagName.H1, TagName.H2, TagName.H3, TagName.H4,
                TagName.H5, TagName.H6, TagName.Head, TagName.Header, TagName.Hgroup,
                TagName.Hr, TagName.Html, TagName.Iframe, TagName.Img, TagName.Input,
                TagName.Keygen, TagName.Li, TagName.Link, TagName.Listing, TagName.Main,
                TagName.Marquee, TagName.Menu, TagName.Meta, TagName.Nav, TagName.Noembed,
                TagName.Noframes, TagName.Noscript, TagName.Object, TagName.Ol, TagName.P,
                TagName.Param, TagName.Plaintext, TagName.Pre, TagName.Script, TagName.Search,
                TagName.Section, TagName.Select, TagName.Source, TagName.Style, TagName.Summary,
                TagName.Table, TagName.Tbody, TagName.Td, TagName.Template, TagName.Textarea,
                TagName.Tfoot, TagName.Th, TagName.Thead, TagName.Title, TagName.Tr, TagName.Track,
                TagName.Ul, TagName.Wbr, TagName.Xmp
            )
            || node.IsOneOf(
                MathML.TagName.Mi, MathML.TagName.Mo, MathML.TagName.Mn, MathML.TagName.Ms,
                MathML.TagName.Mtext, MathML.TagName.Annotation_xml
            )
            || node.IsOneOf(
                SVG.TagName.ForeignObject, SVG.TagName.Desc, SVG.TagName.Title
            )
        );
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