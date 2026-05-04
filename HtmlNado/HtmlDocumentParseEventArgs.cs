namespace HtmlNado;

public class HtmlDocumentParseEventArgs : CancelEventArgs
{
    public HtmlDocumentParseEventArgs(HtmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        Reader = reader;
    }

    public HtmlReader Reader { get; }
    public virtual Encoding? DetectedEncoding { get; set; }
    public virtual HtmlNode? CurrentNode { get; set; }
    public virtual HtmlAttribute? CurrentAttribute { get; set; }
    public virtual bool Continue { get; set; }
}
