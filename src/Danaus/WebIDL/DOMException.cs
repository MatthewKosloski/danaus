namespace Danaus.WebIDL;

public enum DOMExceptionCodeName
{
    INDEX_SIZE_ERR = 1,
    DOMSTRING_SIZE_ERR,
    HIERARCHY_REQUEST_ERR,
    WRONG_DOCUMENT_ERR,
    INVALID_CHARACTER_ERR,
    NO_DATA_ALLOWED_ERR,
    NO_MODIFICATION_ALLOWED_ERR,
    NOT_FOUND_ERR,
    NOT_SUPPORTED_ERR,
    INUSE_ATTRIBUTE_ERR,
    INVALID_STATE_ERR,
    SYNTAX_ERR,
    INVALID_MODIFICATION_ERR,
    NAMESPACE_ERR,
    INVALID_ACCESS_ERR,
    VALIDATION_ERR,
    TYPE_MISMATCH_ERR,
    SECURITY_ERR,
    NETWORK_ERR,
    ABORT_ERR,
    URL_MISMATCH_ERR,
    QUOTA_EXCEEDED_ERR,
    TIMEOUT_ERR,
    INVALID_NODE_TYPE_ERR,
    DATA_CLONE_ERR,
}

// https://webidl.spec.whatwg.org/#idl-DOMException
[Serializable]
public class DOMException(string message, string name) : Exception(message)
{
    public string Name { get; } = name;

    // https://webidl.spec.whatwg.org/#dom-domexception-code
    public int Code
    {
        get
        {
            var nameToEnumValueMap = new Dictionary<string, DOMExceptionCodeName>
            {
                { "IndexSizeError", DOMExceptionCodeName.INDEX_SIZE_ERR },
                { "DomStringSizeError", DOMExceptionCodeName.DOMSTRING_SIZE_ERR },
                { "HierarchyRequestError", DOMExceptionCodeName.HIERARCHY_REQUEST_ERR },
                { "WrongDocumentError", DOMExceptionCodeName.WRONG_DOCUMENT_ERR },
                { "InvalidCharacterError", DOMExceptionCodeName.INVALID_CHARACTER_ERR },
                { "NoDataAllowedError", DOMExceptionCodeName.NO_DATA_ALLOWED_ERR },
                { "NoModificationAllowedError", DOMExceptionCodeName.NO_MODIFICATION_ALLOWED_ERR },
                { "NotFoundError", DOMExceptionCodeName.NOT_FOUND_ERR },
                { "NotSupportedError", DOMExceptionCodeName.NOT_SUPPORTED_ERR },
                { "InUseAttributeError", DOMExceptionCodeName.INUSE_ATTRIBUTE_ERR },
                { "InvalidStateError", DOMExceptionCodeName.INVALID_STATE_ERR },
                { "SyntaxError", DOMExceptionCodeName.SYNTAX_ERR },
                { "InvalidModificationError", DOMExceptionCodeName.INVALID_MODIFICATION_ERR },
                { "NamespaceError", DOMExceptionCodeName.NAMESPACE_ERR },
                { "InvalidAccessError", DOMExceptionCodeName.INVALID_ACCESS_ERR },
                { "ValidationError", DOMExceptionCodeName.VALIDATION_ERR },
                { "TypeMismatchError", DOMExceptionCodeName.TYPE_MISMATCH_ERR },
                { "SecurityError", DOMExceptionCodeName.SECURITY_ERR },
                { "NetworkError", DOMExceptionCodeName.NETWORK_ERR },
                { "AbortError", DOMExceptionCodeName.ABORT_ERR },
                { "UrlMismatchError", DOMExceptionCodeName.URL_MISMATCH_ERR },
                { "QuotaExceededError", DOMExceptionCodeName.QUOTA_EXCEEDED_ERR },
                { "TimeoutError", DOMExceptionCodeName.TIMEOUT_ERR },
                { "InvalidNodeTypeError", DOMExceptionCodeName.INVALID_NODE_TYPE_ERR },
                { "DataCloneError", DOMExceptionCodeName.DATA_CLONE_ERR },
                { "EncodingError", 0 },
                { "NotReadableError", 0 },
                { "UnknownError", 0 },
                { "ConstraintError", 0 },
                { "DataError", 0 },
                { "TransactionInactiveError", 0 },
                { "ReadOnlyError", 0 },
                { "VersionError", 0 },
                { "OperationError", 0 },
                { "NotAllowedError", 0 },
                { "OptOutError", 0 },
            };

            if (nameToEnumValueMap.TryGetValue(Name, out DOMExceptionCodeName result))
            {
                return (int)result;
            }
            else
            {
                return 0; 
            }
        }
    }

}