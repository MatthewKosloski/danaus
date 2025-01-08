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
}