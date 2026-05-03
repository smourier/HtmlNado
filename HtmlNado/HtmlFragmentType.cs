namespace HtmlNado;

// NOTE: keep in sync with HtmlParserState
public enum HtmlFragmentType
{
    Text,
    TagOpen,    // <
    TagEnd,     // -> TagEnd
    TagEndClose,    // />
    TagClose,   // </body
    AttName,
    AttValue,
    Comment,
    CDataText,
}
