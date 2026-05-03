namespace HtmlNado.Utilities;

public class WebFileCacheClient : WebClient
{
    private WebResponse _response;

    public WebFileCacheClient()
    {
        Cookies = new CookieContainer();
        IfModifiedSince = DateTime.MinValue;
        RequestTimeout = -1;
        ReadWriteTimeout = -1;
    }

    public CookieContainer Cookies { get; }
    public DateTime IfModifiedSince { get; set; }
    public int RequestTimeout { get; set; }
    public int ReadWriteTimeout { get; set; }
    public Uri ResponseUri => _response?.ResponseUri;

    protected override WebRequest GetWebRequest(Uri address)
    {
        var request = (HttpWebRequest)base.GetWebRequest(address);
        request.CookieContainer = Cookies;
        request.IfModifiedSince = IfModifiedSince;
        if (RequestTimeout > 0)
        {
            request.Timeout = RequestTimeout;
        }

        if (ReadWriteTimeout > 0)
        {
            request.ReadWriteTimeout = ReadWriteTimeout;
        }
        return request;
    }

    [DebuggerNonUserCode] // avoid catching 304 and breaking here for nothing...
    protected override WebResponse GetWebResponse(WebRequest request)
    {
        _response = base.GetWebResponse(request);
        return _response;
    }
}
