namespace HtmlNado.Utilities;

public class WebFileCacheEventArgs : CancelEventArgs
{
    public WebFileCacheEventArgs(WebFileCacheClient client, WebFileCacheRequestOptions options, string contentUrl)
    {
        ArgumentNullException.ThrowIfNull(client);

        ArgumentNullException.ThrowIfNull(options);

        ArgumentNullException.ThrowIfNull(contentUrl);

        Client = client;
        Options = options;
        ContentUrl = contentUrl;
    }

    public WebFileCacheClient Client { get; }
    public WebFileCacheRequestOptions Options { get; }
    public string ContentUrl { get; set; }
    public bool ContentWasUpdated { get; set; } // only used if cancel is set to true
}
