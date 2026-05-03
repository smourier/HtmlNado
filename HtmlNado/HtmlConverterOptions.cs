namespace HtmlNado;

[Flags]
public enum HtmlConverterOptions
{
    None = 0x0,
    IncludeTitle = 0x1,
    IncludeHeadings = 0x2,
    DontSkipSelfLinks = 0x4,
}
