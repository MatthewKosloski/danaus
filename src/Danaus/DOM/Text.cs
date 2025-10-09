namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#text
class Text(Document document, string data = ""): CharacterData(document, data)
{}