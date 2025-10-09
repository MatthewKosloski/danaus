namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#comment
sealed class Comment(Document document, string data): CharacterData(document, data)
{
}