namespace HtmlNado.Utilities;

public class WebFileCacheRequestOptions
{
    public virtual bool DontUseETag { get; set; }
    public virtual bool DontUseLastModified { get; set; }
    public virtual bool DontForceServerCheck { get; set; }
    public virtual bool AutoDecodeContent { get; set; }
    public virtual bool UseCacheFile { get; set; }
    public virtual int RequestTimeout { get; set; }
    public virtual int ReadWriteTimeout { get; set; }
}
