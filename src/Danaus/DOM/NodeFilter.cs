namespace Danaus.DOM;

// https://dom.spec.whatwg.org/#callbackdef-nodefilter
public sealed class NodeFilter
{
    public const ushort FILTER_ACCEPT = 1;
    public const ushort FILTER_REJECT = 2;
    public const ushort FILTER_SKIP = 3;

    public const ulong SHOW_ALL = 0xFFFFFFFF;
    public const ulong SHOW_ELEMENT = 0x1;
    public const ulong SHOW_ATTRIBUTE = 0x2;
    public const ulong SHOW_TEXT = 0x4;
    public const ulong SHOW_CDATA_SECTION = 0x8;
    public const ulong SHOW_ENTITY_REFERENCE = 0x10; // legacy
    public const ulong SHOW_ENTITY = 0x20; // legacy
    public const ulong SHOW_PROCESSING_INSTRUCTION = 0x40;
    public const ulong SHOW_COMMENT = 0x80;
    public const ulong SHOW_DOCUMENT = 0x100;
    public const ulong SHOW_DOCUMENT_TYPE = 0x200;
    public const ulong SHOW_DOCUMENT_FRAGMENT = 0x400;
    public const ulong SHOW_NOTATION = 0x800; // legacy
}