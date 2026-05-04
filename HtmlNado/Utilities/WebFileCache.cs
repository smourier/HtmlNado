namespace HtmlNado.Utilities;

public class WebFileCache
{
    private static readonly ConcurrentDictionary<string, WaitHandle> _downloadRequests = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<WebFileCacheEventArgs> ClientRequestSetup;

    public WebFileCache(string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);

        DirectoryPath = directoryPath;
    }

    public static void AddContentTypeExtension(string contentType, string ext)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        IOUtilities.AddExtensionsByContentType(contentType, ext);
    }

    public string DirectoryPath { get; }
    public virtual ILogger Logger { get; set; }

    public virtual void DeleteAll() => IOUtilities.DirectoryDelete(DirectoryPath);

    public virtual async Task<bool> DeleteAsync(string url)
    {
        var md = await MetadataFile.GetAsync(this, url, false).ConfigureAwait(false);
        if (md == null)
            return false;

        md.Delete();
        return true;
    }

    protected virtual string ComputeId(string url) => Conversions.ComputeGuidHash(url).ToString("N");

    public virtual async Task<WebFileCacheInfo> GetDownloadedFileInfo(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        var md = await MetadataFile.GetAsync(this, url, false).ConfigureAwait(false);
        return ToInfo(md);
    }

    private WebFileCacheInfo ToInfo(MetadataFile md)
    {
        if (md == null)
            return null;

        var info = new WebFileCacheInfo();
        info.Id = md.Id;
        info.MetadataFilePath = md._filePath;
        info.DataFilePath = md.FilePath;
        info.FileName = md.FileName;
        info.ETag = md.ETag;
        info.ContentType = md.ContentType;
        info.ContentEncoding = md.ContentEncoding;
        info.LastModified = md.LastModified;
        info.Status = md.Status;
        info.Error = md.Error;
        info.HttpStatus = md.HttpStatus;
        info.Expires = md.Expires;
        return info;
    }

    public Task<string> DownloadAsync(string url) => DownloadAsync(url, null);
    public async virtual Task<string> DownloadAsync(string url, WebFileCacheRequestOptions options)
    {
        var info = await DownloadFileAsync(url, options).ConfigureAwait(false);
        if (info == null)
            return null;

        return info.DataFilePath;
    }

    public Task<WebFileCacheInfo> DownloadFileAsync(string url) => DownloadFileAsync(url, null);
    public async virtual Task<WebFileCacheInfo> DownloadFileAsync(string url, WebFileCacheRequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(url);

        options ??= new WebFileCacheRequestOptions();

        if (options.UseCacheFile)
        {
            var md = await GetDownloadedFileInfo(url).ConfigureAwait(false);
            if (md != null && md.DataFilePath != null && IOUtilities.FileExists(md.DataFilePath))
                return md;
        }

        var key = url + '\0' + options;

        var evt = new AutoResetEvent(false);
        var existing = _downloadRequests.AddOrUpdate(key, evt, (k, o) => o);
        var assignedToUs = existing == evt;
        if (!assignedToUs)
        {
            // someone is doing it, wait
            while (_downloadRequests.TryGetValue(key, out WaitHandle wait))
            {
                if (wait.WaitOne(1000)) // wait 1 sec
                    break;
            }

            // if we're there if means someone signaled, or the queue has been emptied, so we don't check with the server
            options.DontForceServerCheck = true;
        }

        try
        {
            var md = await MetadataFile.GetAsync(this, url, true).ConfigureAwait(false);
            var updated = await md.DownloadAsync(url, options).ConfigureAwait(false);
            return ToInfo(md);
        }
        finally
        {
            if (assignedToUs)
            {
                evt.Set();
                _downloadRequests.TryRemove(key, out existing);
            }
        }
    }

    protected void LogInfo(object value, [CallerMemberName] string methodName = null) => Log(TraceLevel.Info, value, methodName);
    protected virtual void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null) => Logger?.Log(level, value, methodName);

    protected virtual void OnClientRequestSetup(object sender, WebFileCacheEventArgs e) => ClientRequestSetup?.Invoke(this, e);

    private class MetadataFile
    {
        private const char _lineFeedReplacement = '\u2028'; // Line Separator

        private readonly Dictionary<string, string> _dictionary = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _initialDictionary;
        internal string _filePath;
        private readonly WebFileCache _cache;

        private MetadataFile(WebFileCache cache, string filePath)
        {
            _cache = cache;
            _filePath = filePath;
        }

        public static async Task<MetadataFile> GetAsync(WebFileCache cache, string url, bool createIfNotExists)
        {
            var id = cache.ComputeId(url);
            MetadataFile file;
            var filePath = Path.Combine(cache.DirectoryPath, id + ".txt");
            if (!IOUtilities.FileExists(filePath))
            {
                if (!createIfNotExists)
                    return null;

                file = new MetadataFile(cache, filePath);
            }
            else
            {
                file = new MetadataFile(cache, filePath);
                using var sr = new StreamReader(filePath);
                do
                {
                    var line = await sr.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                        break;

                    string key;
                    string value;
                    int pos = line.IndexOf(':');
                    if (pos < 0)
                    {
                        key = line;
                        value = null;
                    }
                    else
                    {
                        key = line[..pos];
                        value = line[(pos + 1)..].Trim();
                        value = value.Replace(_lineFeedReplacement.ToString(CultureInfo.InvariantCulture), Environment.NewLine);
                    }

                    key = key.Trim();
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    file._dictionary.Add(key, value);
                }
                while (true);
            }

            file.Commit();
            file.Id = id;
            return file;
        }

        public string Id { get; private set; }
        public string ETag => _dictionary.GetNullifiedValue(nameof(ETag));
        public string ContentType => _dictionary.GetNullifiedValue(nameof(ContentType));
        public string ContentEncoding => _dictionary.GetNullifiedValue(nameof(ContentEncoding));
        public DateTime LastModified => _dictionary.GetValue(nameof(LastModified), DateTime.MinValue);
        public DateTime Expires => _dictionary.GetValue(nameof(Expires), DateTime.MinValue);
        public string FileName => _dictionary.GetNullifiedValue(nameof(FileName));
        public string FilePath => FileName != null ? Path.Combine(Path.GetDirectoryName(_filePath), Id + "." + FileName) : null;
        public HttpStatusCode HttpStatus => _dictionary.GetValue(nameof(HttpStatus), HttpStatusCode.OK);
        public WebExceptionStatus Status => _dictionary.GetValue(nameof(Status), WebExceptionStatus.Success);
        public string Error => _dictionary.GetNullifiedValue(nameof(Error));
        public bool HasChanged => !_dictionary.Compare(_initialDictionary);

        private void Commit()
        {
            _initialDictionary = new Dictionary<string, string>(_dictionary.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _dictionary)
            {
                _initialDictionary.Add(kv.Key, kv.Value);
            }
        }

        public void Delete() => IOUtilities.WrapSharingViolations(() =>
                              {
                                  var filePath = FilePath;
                                  if (filePath != null)
                                  {
                                      IOUtilities.FileDelete(filePath, true);
                                  }
                                  IOUtilities.FileDelete(_filePath);
                              });

        public bool Write()
        {
            if (!HasChanged)
                return false;

            IOUtilities.FileCreateDirectory(_filePath);
            using (var writer = new StreamWriter(_filePath, false, Encoding.UTF8))
            {
                foreach (var entry in _dictionary)
                {
                    writer.Write(entry.Key);
                    writer.Write(':');

                    var value = entry.Value;
                    if (value != null)
                    {
                        value = value.Replace("\r", string.Empty).Replace('\n', _lineFeedReplacement);
                    }
                    writer.WriteLine(value);
                }
            }
            Commit();
            return true;
        }

        public async Task<string> HandleContentEncoding(string contentEncoding, string tmpFile)
        {
            if (contentEncoding.EqualsIgnoreCase("gzip"))
            {
                var gzFilePath = Path.GetTempFileName();
                using (var gzFile = File.OpenRead(tmpFile))
                {
                    IOUtilities.FileCreateDirectory(gzFilePath);
                    IOUtilities.FileDelete(gzFilePath);
                    using (var outFile = File.OpenWrite(gzFilePath))
                    {
                        using var gz = new GZipStream(gzFile, CompressionMode.Decompress);
                        await gz.CopyToAsync(outFile).ConfigureAwait(false);
                    }

                    _cache.LogInfo("Gzip was extracted at '" + gzFilePath + "'. Size: " + new FileInfo(gzFilePath).Length + " byte(s).");
                }

                IOUtilities.FileDelete(tmpFile);
                return gzFilePath;
            }

            if (contentEncoding.EqualsIgnoreCase("deflate"))
            {
                var deflatedFilePath = Path.GetTempFileName();
                using (var deflatedFile = File.OpenRead(tmpFile))
                {
                    IOUtilities.FileCreateDirectory(deflatedFilePath);
                    IOUtilities.FileDelete(deflatedFilePath);
                    using (var outFile = File.OpenWrite(deflatedFilePath))
                    {
                        using var def = new DeflateStream(deflatedFile, CompressionMode.Decompress);
                        await def.CopyToAsync(outFile).ConfigureAwait(false);
                    }

                    _cache.LogInfo("Deflate was extracted at '" + deflatedFilePath + "'. Size: " + new FileInfo(deflatedFilePath).Length + " byte(s).");
                }

                IOUtilities.FileDelete(tmpFile);
                return deflatedFilePath;
            }

            return null;
        }

        public async Task<bool> DownloadAsync(string contentUrl, WebFileCacheRequestOptions options)
        {
            ArgumentNullException.ThrowIfNull(contentUrl);

            var filePath = FilePath;
            _cache.LogInfo("Download '" + filePath + "' Exists: " + (filePath != null && IOUtilities.FileExists(filePath)) + " Expires: " + Expires + " DontCheckServer: " + options.DontForceServerCheck + ".");
            if (options.DontForceServerCheck && filePath != null && IOUtilities.FileExists(filePath) && Expires < DateTime.Now)
                return false;

            if (filePath == null || !IOUtilities.FileExists(filePath))
            {
                options.DontUseETag = true;
            }

            // build a unique per-request name
            var tmpFile = Path.Combine(Path.GetDirectoryName(_filePath), Path.GetFileNameWithoutExtension(_filePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            IOUtilities.FileCreateDirectory(tmpFile);
            IOUtilities.FileDelete(tmpFile);
            using var client = new WebFileCacheClient();
            if (options.RequestTimeout > 0)
            {
                client.RequestTimeout = options.RequestTimeout;
            }

            if (options.ReadWriteTimeout > 0)
            {
                client.ReadWriteTimeout = options.ReadWriteTimeout;
            }

            if (!options.DontUseETag)
            {
                var etag = ETag;
                if (etag != null)
                {
                    client.Headers.Add(HttpRequestHeader.IfNoneMatch, etag);
                }
            }

            if (!options.DontUseLastModified)
            {
                var lm = LastModified;
                if (lm != DateTime.MinValue)
                {
                    client.IfModifiedSince = lm;
                }
            }

            var e = new WebFileCacheEventArgs(client, options, contentUrl);
            _cache.OnClientRequestSetup(_cache, e);
            if (e.Cancel)
                return e.ContentWasUpdated;

            if (!string.IsNullOrWhiteSpace(e.ContentUrl))
            {
                contentUrl = e.ContentUrl;
            }

            var updated = false;
            try
            {
                await client.DownloadFileTaskAsync(contentUrl, tmpFile).ConfigureAwait(false);

                var ct = client.ResponseHeaders[HttpResponseHeader.ContentType].Nullify();
                if (string.Equals(ct, IOUtilities.ApplicationOctetStream, StringComparison.OrdinalIgnoreCase))
                {
                    ct = null;
                }

                var cf = client.ResponseHeaders["Content-Disposition"].Nullify();
                var fileName = Extensions.UnencodeUTF8(Extensions.GetAttributeFromHeader(cf, "filename"));
                if (fileName == null)
                {
                    _cache.LogInfo(TraceLevel.Warning, "HTTP Content-Disposition/filename was not found in headers resulting from GET '" + contentUrl + "'.");
                    fileName = new Uri(contentUrl).Segments.LastOrDefault();
                    if (string.Equals(fileName, "/", StringComparison.Ordinal))
                    {
                        fileName = null;
                    }
                    fileName ??= "default";
                }

                var ext = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(ext))
                {
                    // too bad. let's sniff this stuff out
                    ct ??= IOUtilities.FindContentType(tmpFile);

                    ext = IOUtilities.GetFileExtensionFromContentType(ct);
                    if (!string.IsNullOrWhiteSpace(ext))
                    {
                        fileName += ext;
                    }
                }

                _dictionary.Clear();

                var newETag = client.ResponseHeaders[HttpResponseHeader.ETag].Nullify();
                if (newETag == null)
                {
                    _dictionary.Remove(nameof(ETag));
                }
                else
                {
                    _dictionary[nameof(ETag)] = newETag;
                }

                var newLm = client.ResponseHeaders[HttpResponseHeader.LastModified].Nullify();
                if (newLm == null || !DateTime.TryParse(newLm, out var nl))
                {
                    _dictionary.Remove(nameof(LastModified));
                }
                else
                {
                    _dictionary[nameof(LastModified)] = newLm;
                }

                // https://tools.ietf.org/html/rfc2616#section-14.21
                var expires = client.ResponseHeaders[HttpResponseHeader.Expires].Nullify();
                if (expires == null || !DateTime.TryParse(expires, out var exp))
                {
                    _dictionary.Remove(nameof(Expires));
                }
                else
                {
                    _dictionary[nameof(Expires)] = expires;
                }

                if (ct == null)
                {
                    _dictionary.Remove(nameof(ContentType));
                }
                else
                {
                    _dictionary[nameof(ContentType)] = ct;
                }

                var encoding = client.ResponseHeaders[HttpResponseHeader.ContentEncoding].Nullify();
                if (encoding == null)
                {
                    _dictionary.Remove(nameof(ContentEncoding));
                }
                else
                {
                    _dictionary[nameof(ContentEncoding)] = encoding;
                    if (options.AutoDecodeContent)
                    {
                        var newTmpFile = await HandleContentEncoding(encoding, tmpFile).ConfigureAwait(false);
                        if (newTmpFile != null)
                        {
                            tmpFile = newTmpFile;

                            // remember where we came from
                            _dictionary["Original-ContentEncoding"] = encoding;
                            _dictionary.Remove(nameof(ContentEncoding));
                        }
                    }
                }

                _dictionary[nameof(FileName)] = fileName;
                IOUtilities.FileMove(tmpFile, FilePath, true);
                _cache.LogInfo("HTTP GET '" + FilePath + "' was written.");
                if (!string.IsNullOrWhiteSpace(ETag) || !string.IsNullOrWhiteSpace(newETag))
                {
                    _cache.LogInfo("Old etag: '" + ETag + "'. New etag: '" + newETag + "'.");
                }

                if (LastModified != DateTime.MinValue || !string.IsNullOrWhiteSpace(newLm))
                {
                    _cache.LogInfo("Old last-modified: '" + LastModified + "'. New last-modified: '" + newLm + "'.");
                }
                updated = true;
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response == null || response.StatusCode != HttpStatusCode.NotModified)
                {
                    _dictionary[nameof(Status)] = ((int)ex.Status).ToString(CultureInfo.InvariantCulture);
                    string? error = null;
                    if (response != null)
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            using var sr = new StreamReader(stream);
                            error = await sr.ReadToEndAsync().ConfigureAwait(false);
                        }

                        _dictionary[nameof(HttpStatus)] = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
                        if (error == null)
                        {
                            _dictionary.Remove(nameof(Error));
                        }
                        else
                        {
                            _dictionary[nameof(Error)] = error;
                        }
                    }

                    string? httpError = null;
                    var httpStatus = response?.StatusCode;
                    if (httpStatus.HasValue)
                    {
                        httpError = " HTTP Status: " + httpStatus.Value + " (" + ((int)httpStatus.Value) + ")";
                    }

                    if (error != null)
                    {
                        error = error.Replace("\r", string.Empty).Replace('\n', ' ');
                        _cache.Log(TraceLevel.Error, "HTTP GET '" + contentUrl + "' Status: " + ex.Status + httpError + " Error: " + ex.Message + error);
                    }
                    else
                    {
                        _cache.Log(TraceLevel.Error, "HTTP GET '" + contentUrl + "'. Status: " + ex.Status + httpError);
                    }
                }
                else
                {
                    _cache.LogInfo("HTTP GET '" + contentUrl + " was not modified.");
                }
#pragma warning disable MA0042 // Do not use blocking call
                IOUtilities.FileDelete(tmpFile);
#pragma warning restore MA0042 // Do not use blocking call
            }
            Write();
            return updated;
        }
    }
}
