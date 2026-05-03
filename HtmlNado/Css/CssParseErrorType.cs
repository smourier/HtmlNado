namespace HtmlNado.Css;

public enum CssParseErrorType
{
    Unspecified,
    TokenizerBadUrl,
    TokenizerBadString,
    TokenizerBadEscape,
    ParserColonExpected,
    ParserUnexpectedToken,
    ParserUnexpectedEof,
}
