namespace Danaus.Url;

public class ParseResult(URL url, List<ValidationError>? validationErrors = null)
{
    public URL Url { get; } = url;
    public IList<ValidationError> ValidationErrors { get; } = validationErrors ?? [];

    public void AddError(ValidationError error)
    {
        if (!ValidationErrors.Contains(error))
        {
            ValidationErrors.Add(error);
        }
    }

    public bool IsFailure()
    {
        return ValidationErrors is not null;
    }

    public override bool Equals(Object? other)
    {
        if (other == null || !(other is ParseResult))
        {
            return false;
        }
        else
        {
            var otherParseResult = (ParseResult)other;
            return otherParseResult.Url.Equals(Url) && otherParseResult.ValidationErrors.SequenceEqual(ValidationErrors);
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Url.GetHashCode(), ValidationErrors.GetHashCode());
    }

}