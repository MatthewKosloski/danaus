namespace Danaus.Url;

// https://url.spec.whatwg.org/#validation-error
public enum ValidationError
{
    DomainToASCII,
    DomainInvalidCodePoint,
    DomainToUnicode,
    HostInvalidCodePoint,
    InvalidURLUnit,
    SpecialSchemeMissingFollowingSolidus,
    MissingSchemeNonRelativeURL,
    InvalidReverseSolidus,
    InvalidCredentials,
    HostMissing,
    PortOutOfRange,
    PortInvalid,
    FileInvalidWindowsDriveLetter,
    FileInvalidWindowsDriveLetterHost,
}