namespace HtmlNado.Utilities;

public class WebFileCacheInfo
{
    internal WebFileCacheInfo()
    {
    }

    public string Id { get; internal set; }
    public string MetadataFilePath { get; internal set; }
    public string DataFilePath { get; internal set; }
    public string FileName { get; internal set; }
    public string ETag { get; internal set; }
    public string ContentType { get; internal set; }
    public string ContentEncoding { get; internal set; }
    public DateTime LastModified { get; internal set; }
    public DateTime Expires { get; internal set; }
    public HttpStatusCode HttpStatus { get; internal set; }
    public WebExceptionStatus Status { get; internal set; }
    public string Error { get; internal set; }
}
