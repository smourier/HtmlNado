namespace HtmlNado.Utilities;

public static class IOUtilities
{
    private static readonly string[] _reservedFileNames = new[]
{
        "con", "prn", "aux", "nul",
        "com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    };

    public const int DefaultWrapSharingViolationsRetryCount = 10;
    public const int DefaultWrapSharingViolationsWaitTime = 100;
    public const string ApplicationOctetStream = "application/octet-stream";

    private static readonly ConcurrentDictionary<string, string> _extensionsByContentType = new(StringComparer.OrdinalIgnoreCase);

    static IOUtilities()
    {
        AddExtensionsByContentType("application/brushes", ".azb");
        AddExtensionsByContentType("application/brushtip", ".paj");
        AddExtensionsByContentType("application/epub+zip", ".epub");
        AddExtensionsByContentType("application/fractals", ".fif");
        AddExtensionsByContentType("application/futuresplash", ".spl");
        AddExtensionsByContentType("application/hta", ".hta");
        AddExtensionsByContentType("application/journal", ".sja");
        AddExtensionsByContentType("application/mac-binhex40", ".hqx");
        AddExtensionsByContentType("application/ms-vsi", ".vsi");
        AddExtensionsByContentType("application/msaccess", ".accdb");
        AddExtensionsByContentType("application/msaccess.addin", ".accda");
        AddExtensionsByContentType("application/msaccess.cab", ".accdc");
        AddExtensionsByContentType("application/msaccess.exec", ".accde");
        AddExtensionsByContentType("application/msaccess.ftemplate", ".accft");
        AddExtensionsByContentType("application/msaccess.runtime", ".accdr");
        AddExtensionsByContentType("application/msaccess.template", ".accdt");
        AddExtensionsByContentType("application/msaccess.webapplication", ".accdw");
        AddExtensionsByContentType("application/msonenote", ".one");
        AddExtensionsByContentType("application/msword", ".doc");
        AddExtensionsByContentType("application/opensearchdescription+xml", ".osdx");
        AddExtensionsByContentType("application/oxps", ".oxps");
        AddExtensionsByContentType("application/papertexture", ".ptj");
        AddExtensionsByContentType("application/pdf", ".pdf");
        AddExtensionsByContentType("application/pkcs10", ".p10");
        AddExtensionsByContentType("application/pkcs7-mime", ".p7c");
        AddExtensionsByContentType("application/pkcs7-signature", ".p7s");
        AddExtensionsByContentType("application/pkix-cert", ".cer");
        AddExtensionsByContentType("application/pkix-crl", ".crl");
        AddExtensionsByContentType("application/postscript", ".ps");
        AddExtensionsByContentType("application/tif", ".tif");
        AddExtensionsByContentType("application/tiff", ".tiff");
        AddExtensionsByContentType("application/vnd.adobe.acrobat-security-settings", ".acrobatsecuritysettings");
        AddExtensionsByContentType("application/vnd.adobe.pdfxml", ".pdfxml");
        AddExtensionsByContentType("application/vnd.adobe.pdx", ".pdx");
        AddExtensionsByContentType("application/vnd.adobe.xdp+xml", ".xdp");
        AddExtensionsByContentType("application/vnd.adobe.xfd+xml", ".xfd");
        AddExtensionsByContentType("application/vnd.adobe.xfdf", ".xfdf");
        AddExtensionsByContentType("application/vnd.fdf", ".fdf");
        AddExtensionsByContentType("application/vnd.ms-excel", ".xls");
        AddExtensionsByContentType("application/vnd.ms-excel.12", ".xlsx");
        AddExtensionsByContentType("application/vnd.ms-excel.addin.macroEnabled.12", ".xlam");
        AddExtensionsByContentType("application/vnd.ms-excel.sheet.binary.macroEnabled.12", ".xlsb");
        AddExtensionsByContentType("application/vnd.ms-excel.sheet.macroEnabled.12", ".xlsm");
        AddExtensionsByContentType("application/vnd.ms-excel.template.macroEnabled.12", ".xltm");
        AddExtensionsByContentType("application/vnd.ms-officetheme", ".thmx");
        AddExtensionsByContentType("application/vnd.ms-pki.certstore", ".sst");
        AddExtensionsByContentType("application/vnd.ms-pki.pko", ".pko");
        AddExtensionsByContentType("application/vnd.ms-pki.seccat", ".cat");
        AddExtensionsByContentType("application/vnd.ms-powerpoint", ".ppt");
        AddExtensionsByContentType("application/vnd.ms-powerpoint.12", ".pptx");
        AddExtensionsByContentType("application/vnd.ms-powerpoint.addin.macroEnabled.12", ".ppam");
        AddExtensionsByContentType("application/vnd.ms-powerpoint.presentation.macroEnabled.12", ".pptm");
        AddExtensionsByContentType("application/vnd.ms-powerpoint.slide.macroEnabled.12", ".sldm");
        AddExtensionsByContentType("application/vnd.ms-powerpoint.slideshow.macroEnabled.12", ".ppsm");
        AddExtensionsByContentType("application/vnd.ms-powerpoint.template.macroEnabled.12", ".potm");
        AddExtensionsByContentType("application/vnd.ms-publisher", ".pub");
        AddExtensionsByContentType("application/vnd.ms-visio.viewer", ".vdx");
        AddExtensionsByContentType("application/vnd.ms-word.document.12", ".docx");
        AddExtensionsByContentType("application/vnd.ms-word.document.macroEnabled.12", ".docm");
        AddExtensionsByContentType("application/vnd.ms-word.template.12", ".dotx");
        AddExtensionsByContentType("application/vnd.ms-word.template.macroEnabled.12", ".dotm");
        AddExtensionsByContentType("application/vnd.ms-wpl", ".wpl");
        AddExtensionsByContentType("application/vnd.ms-xpsdocument", ".xps");
        AddExtensionsByContentType("application/vnd.oasis.opendocument.presentation", ".odp");
        AddExtensionsByContentType("application/vnd.oasis.opendocument.spreadsheet", ".ods");
        AddExtensionsByContentType("application/vnd.oasis.opendocument.text", ".odt");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.presentationml.slide", ".sldx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.presentationml.slideshow", ".ppsx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.presentationml.template", ".potx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.template", ".xltx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx");
        AddExtensionsByContentType("application/vnd.openxmlformats-officedocument.wordprocessingml.template", ".dotx");
        AddExtensionsByContentType("application/windows-appcontent+xml", ".appcontent-ms");
        AddExtensionsByContentType("application/x-compress", ".z");
        AddExtensionsByContentType("application/x-dtcp1", ".dtcp-ip");
        AddExtensionsByContentType("application/x-gzip", ".gz");
        AddExtensionsByContentType("application/x-latex", ".latex");
        AddExtensionsByContentType("application/x-mix-transfer", ".nix");
        AddExtensionsByContentType("application/x-mplayer2", ".asx");
        AddExtensionsByContentType("application/x-ms-application", ".application");
        AddExtensionsByContentType("application/x-ms-vsto", ".vsto");
        AddExtensionsByContentType("application/x-ms-wmd", ".wmd");
        AddExtensionsByContentType("application/x-ms-wmz", ".wmz");
        AddExtensionsByContentType("application/x-ms-xbap", ".xbap");
        AddExtensionsByContentType("application/x-mswebsite", ".website");
        AddExtensionsByContentType("application/x-pkcs12", ".p12");
        AddExtensionsByContentType("application/x-pkcs7-certificates", ".p7b");
        AddExtensionsByContentType("application/x-pkcs7-certreqresp", ".p7r");
        AddExtensionsByContentType("application/x-podcast", ".pcast");
        AddExtensionsByContentType("application/x-shockwave-flash", ".swf");
        AddExtensionsByContentType("application/x-skype", ".skype");
        AddExtensionsByContentType("application/x-stuffit", ".sit");
        AddExtensionsByContentType("application/x-tar", ".tar");
        AddExtensionsByContentType("application/x-wmplayer", ".asx");
        AddExtensionsByContentType("application/x-x509-ca-cert", ".cer");
        AddExtensionsByContentType("application/x-zip-compressed", ".zip");
        AddExtensionsByContentType("application/xaml+xml", ".xaml");
        AddExtensionsByContentType("application/xhtml+xml", ".xht");
        AddExtensionsByContentType("application/xml", ".xml");
        AddExtensionsByContentType("application/xps", ".xps");
        AddExtensionsByContentType("application/zip", ".zip");
        AddExtensionsByContentType("audio/3gpp", ".3gp");
        AddExtensionsByContentType("audio/3gpp2", ".3g2");
        AddExtensionsByContentType("audio/aac", ".aac");
        AddExtensionsByContentType("audio/ac3", ".ac3");
        AddExtensionsByContentType("audio/aiff", ".aiff");
        AddExtensionsByContentType("audio/amr", ".amr");
        AddExtensionsByContentType("audio/basic", ".au");
        AddExtensionsByContentType("audio/ec3", ".ec3");
        AddExtensionsByContentType("audio/l16", ".lpcm");
        AddExtensionsByContentType("audio/mid", ".mid");
        AddExtensionsByContentType("audio/midi", ".mid");
        AddExtensionsByContentType("audio/mp3", ".mp3");
        AddExtensionsByContentType("audio/mp4", ".m4a");
        AddExtensionsByContentType("audio/mpeg", ".mp3");
        AddExtensionsByContentType("audio/mpegurl", ".m3u");
        AddExtensionsByContentType("audio/mpg", ".mp3");
        AddExtensionsByContentType("audio/vnd.dlna.adts", ".adts");
        AddExtensionsByContentType("audio/vnd.dolby.dd-raw", ".ac3");
        AddExtensionsByContentType("audio/wav", ".wav");
        AddExtensionsByContentType("audio/webm", ".weba");
        AddExtensionsByContentType("audio/x-aiff", ".aiff");
        AddExtensionsByContentType("audio/x-flac", ".flac");
        AddExtensionsByContentType("audio/x-m4a", ".m4a");
        AddExtensionsByContentType("audio/x-m4r", ".m4r");
        AddExtensionsByContentType("audio/x-matroska", ".mka");
        AddExtensionsByContentType("audio/x-mid", ".mid");
        AddExtensionsByContentType("audio/x-midi", ".mid");
        AddExtensionsByContentType("audio/x-mp3", ".mp3");
        AddExtensionsByContentType("audio/x-mpeg", ".mp3");
        AddExtensionsByContentType("audio/x-mpegurl", ".m3u");
        AddExtensionsByContentType("audio/x-mpg", ".mp3");
        AddExtensionsByContentType("audio/x-ms-wax", ".wax");
        AddExtensionsByContentType("audio/x-ms-wma", ".wma");
        AddExtensionsByContentType("audio/x-wav", ".wav");
        AddExtensionsByContentType("image/bmp", ".dib");
        AddExtensionsByContentType("image/gif", ".gif");
        AddExtensionsByContentType("image/jpeg", ".jpg");
        AddExtensionsByContentType("image/pjpeg", ".jpg");
        AddExtensionsByContentType("image/png", ".png");
        AddExtensionsByContentType("image/svg+xml", ".svg");
        AddExtensionsByContentType("image/tiff", ".tif");
        AddExtensionsByContentType("image/vnd.ms-dds", ".dds");
        AddExtensionsByContentType("image/vnd.ms-photo", ".wdp");
        AddExtensionsByContentType("image/x-emf", ".emf");
        AddExtensionsByContentType("image/x-icon", ".ico");
        AddExtensionsByContentType("image/x-png", ".png");
        AddExtensionsByContentType("image/x-wmf", ".wmf");
        AddExtensionsByContentType("midi/mid", ".mid");
        AddExtensionsByContentType("pkcs10", ".p10");
        AddExtensionsByContentType("pkcs7-mime", ".p7m");
        AddExtensionsByContentType("pkcs7-signature", ".p7s");
        AddExtensionsByContentType("pkix-cert", ".cer");
        AddExtensionsByContentType("pkix-crl", ".crl");
        AddExtensionsByContentType("text/calendar", ".ics");
        AddExtensionsByContentType("text/css", ".css");
        AddExtensionsByContentType("text/directory", ".vcf");
        AddExtensionsByContentType("text/html", ".html");
        AddExtensionsByContentType("text/plain", ".txt");
        AddExtensionsByContentType("text/scriptlet", ".wsc");
        AddExtensionsByContentType("text/vcard", ".vcf");
        AddExtensionsByContentType("text/x-component", ".htc");
        AddExtensionsByContentType("text/x-ms-contact", ".contact");
        AddExtensionsByContentType("text/x-ms-iqy", ".iqy");
        AddExtensionsByContentType("text/x-ms-odc", ".odc");
        AddExtensionsByContentType("text/x-ms-rqy", ".rqy");
        AddExtensionsByContentType("text/x-vcard", ".vcf");
        AddExtensionsByContentType("text/xml", ".xml");
        AddExtensionsByContentType("video/3gpp", ".3gp");
        AddExtensionsByContentType("video/3gpp2", ".3g2");
        AddExtensionsByContentType("video/avi", ".avi");
        AddExtensionsByContentType("video/mp4", ".mp4");
        AddExtensionsByContentType("video/mpeg", ".mpeg");
        AddExtensionsByContentType("video/mpg", ".mpeg");
        AddExtensionsByContentType("video/msvideo", ".avi");
        AddExtensionsByContentType("video/quicktime", ".mov");
        AddExtensionsByContentType("video/webm", ".webm");
        AddExtensionsByContentType("video/wtv", ".wtv");
        AddExtensionsByContentType("video/x-m4v", ".m4v");
        AddExtensionsByContentType("video/x-matroska", ".mkv");
        AddExtensionsByContentType("video/x-mpeg", ".mpeg");
        AddExtensionsByContentType("video/x-mpeg2a", ".mpeg");
        AddExtensionsByContentType("video/x-ms-asf", ".asx");
        AddExtensionsByContentType("video/x-ms-dvr", ".dvr-ms");
        AddExtensionsByContentType("video/x-ms-wm", ".wm");
        AddExtensionsByContentType("video/x-ms-wmv", ".wmv");
        AddExtensionsByContentType("video/x-ms-wmx", ".wmx");
        AddExtensionsByContentType("video/x-ms-wvx", ".wvx");
        AddExtensionsByContentType("video/x-msvideo", ".avi");
        AddExtensionsByContentType("vnd.ms-pki.certstore", ".sst");
        AddExtensionsByContentType("vnd.ms-pki.pko", ".pko");
        AddExtensionsByContentType("vnd.ms-pki.seccat", ".cat");
        AddExtensionsByContentType("x-pkcs12", ".p12");
        AddExtensionsByContentType("x-pkcs7-certificates", ".p7b");
        AddExtensionsByContentType("x-pkcs7-certreqresp", ".p7r");
        AddExtensionsByContentType("x-x509-ca-cert", ".cer");
    }

    internal static void AddExtensionsByContentType(string contentType, string ext) => _extensionsByContentType.AddOrUpdate(contentType, ext, (k, old) => ext);

    [DllImport("urlmon.dll", CharSet = CharSet.Unicode)]
    private extern static int FindMimeFromData(IntPtr pBC,
        [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
        byte[] pBuffer,
        int cbSize,
        string pwzMimeProposed,
        UInt32 dwMimeFlags,
        out IntPtr ppwzMimeOut,
        int dwReserverd
        );

    public static string FindContentType(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        const int FMFD_ENABLEMIMESNIFFING = 0x2;
        const int FMFD_RETURNUPDATEDIMGMIMES = 0x20;
        FindMimeFromData(IntPtr.Zero, null, bytes, bytes.Length, null, FMFD_RETURNUPDATEDIMGMIMES | FMFD_ENABLEMIMESNIFFING, out IntPtr ptr, 0);
        if (ptr == IntPtr.Zero)
            return null;

        var ct = Marshal.PtrToStringUni(ptr);
        Marshal.FreeCoTaskMem(ptr);
        return !string.Equals(ct, ApplicationOctetStream, StringComparison.OrdinalIgnoreCase) ? ct : null;
    }

    public static string FindContentType(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!FileExists(filePath))
            return null;

        var bytes = new byte[256];
        using (var file = File.OpenRead(filePath))
        {
            file.Read(bytes, 0, bytes.Length);
        }
        return FindContentType(bytes);
    }

    public static string PathMakeRelativeIfChild(string path1, string path2)
    {
        if (PathIsChildOrEqual(path1, path2))
            return PathMakeRelative(path1, path2);

        return path2;
    }

    public static string PathMakeRelative(string path1, string path2)
    {
        ArgumentNullException.ThrowIfNull(path1);

        ArgumentNullException.ThrowIfNull(path2);

        var uri1 = new Uri(path1, UriKind.RelativeOrAbsolute);
        if (!uri1.IsAbsoluteUri)
        {
            uri1 = new Uri(Environment.ExpandEnvironmentVariables(path1), UriKind.RelativeOrAbsolute);
        }

        var uri2 = new Uri(path2, UriKind.RelativeOrAbsolute);
        if (!uri2.IsAbsoluteUri)
        {
            uri2 = new Uri(Environment.ExpandEnvironmentVariables(path2), UriKind.RelativeOrAbsolute);
        }

        if (!uri1.IsAbsoluteUri || !uri2.IsAbsoluteUri)
            return path2;

        if (!uri1.Scheme.EqualsIgnoreCase(uri2.Scheme))
            return path2;

        var s = Uri.UnescapeDataString(uri1.MakeRelativeUri(uri2).ToString());
        if (!string.Equals(uri1.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            return s;

        return s.Replace('/', '\\');
    }

    public static string PathToValidFileName(string fileName, string reservedNameFormat = null, string reservedCharFormat = null)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        if (string.IsNullOrWhiteSpace(reservedNameFormat))
        {
            reservedNameFormat = "_{0}_";
        }

        if (string.IsNullOrWhiteSpace(reservedCharFormat))
        {
            reservedCharFormat = "_x{0}_";
        }

        if (Array.IndexOf(_reservedFileNames, fileName.ToLowerInvariant()) >= 0 || IsAllDots(fileName))
            return string.Format(reservedNameFormat, fileName);

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(fileName.Length);
        foreach (char c in fileName)
        {
            if (Array.IndexOf(invalid, c) >= 0)
            {
                sb.AppendFormat(reservedCharFormat, (short)c);
            }
            else
            {
                sb.Append(c);
            }
        }

        var s = sb.ToString();
        if (s.Length >= 255) // a segment is always 255 max even with long file names
        {
            s = s[..254];
        }

        if (s.EqualsIgnoreCase(fileName))
            return fileName;

        return s;
    }

    private static bool IsAllDots(string fileName)
    {
        foreach (char c in fileName)
        {
            if (c != '.')
                return false;
        }
        return true;
    }

    public static string GetFileExtensionFromContentType(string contentType)
    {
        if (contentType == null)
            return null;

        _extensionsByContentType.TryGetValue(contentType, out var ext);
        return ext;
    }

    public static void FileCreateDirectory(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.GetFullPath(filePath);
        }

        var dir = Path.GetDirectoryName(filePath);
        if (dir == null || DirectoryExists(dir))
            return;

        Directory.CreateDirectory(dir);
    }

    public static void FileMove(string source, string destination) => FileMove(source, destination, true);
    public static void FileMove(string source, string destination, bool unprotect)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(destination);

        FileDelete(destination, unprotect);
        FileCreateDirectory(destination);
        File.Move(source, destination);
    }

    public static void DirectoryDelete(string directoryPath) => DirectoryDelete(directoryPath, true);
    public static void DirectoryDelete(string directoryPath, bool recursive)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);

        if (!DirectoryExists(directoryPath))
            return;

        try
        {
            Directory.Delete(directoryPath, recursive);
        }
        catch
        {
            // do nothing
        }
    }

    public static void FileDelete(string filePath) => FileDelete(filePath, true);
    public static void FileDelete(string filePath, bool unprotect)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!FileExists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly && unprotect)
        {
            File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
        }
        File.Delete(filePath);
    }

    public static void FileOverwrite(string source, string destination) => FileOverwrite(source, destination, true);
    public static void FileOverwrite(string source, string destination, bool unprotect)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(source);

        if (PathIsEqual(source, destination))
            return;

        FileDelete(destination, unprotect);
        FileCreateDirectory(destination);
        File.Copy(source, destination, true);
    }

    public static bool FileExists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    public static bool DirectoryExists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    public static bool PathIsEqual(string path1, string path2) => PathIsEqual(path1, path2, true);
    public static bool PathIsEqual(string path1, string path2, bool normalize)
    {
        ArgumentNullException.ThrowIfNull(path1);

        ArgumentNullException.ThrowIfNull(path2);

        if (normalize)
        {
            path1 = Path.GetFullPath(path1);
            path2 = Path.GetFullPath(path2);
        }

        return path1.EqualsIgnoreCase(path2);
    }

    public static bool PathIsChildOrEqual(string path, string child) => PathIsChildOrEqual(path, child, true);
    public static bool PathIsChildOrEqual(string path, string child, bool normalize) => PathIsChild(path, child, normalize) || PathIsEqual(path, child, normalize);
    public static bool PathIsChild(string path, string child) => PathIsChild(path, child, true);
    public static bool PathIsChild(string path, string child, bool normalize) => PathIsChild(path, child, normalize, out _);
    public static bool PathIsChild(string path, string child, bool normalize, out string subPath)
    {
        ArgumentNullException.ThrowIfNull(path);

        ArgumentNullException.ThrowIfNull(child);

        subPath = null;
        if (normalize)
        {
            path = Path.GetFullPath(path);
            child = Path.GetFullPath(child);
        }

        path = StripTerminatingPathSeparators(path);
        if (child.Length < (path.Length + 1))
            return false;

        var newChild = Path.Combine(path, child[(path.Length + 1)..]);
        var b = newChild.EqualsIgnoreCase(child);
        if (b)
        {
            subPath = child[path.Length..];
            while (subPath.StartsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase))
            {
                subPath = subPath[1..];
            }
        }
        return b;
    }

    public static string StripTerminatingPathSeparators(string path)
    {
        if (path == null)
            return null;

        while (path.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.CurrentCulture), StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^1];
        }
        return path;
    }

    public delegate bool WrapSharingViolationsExceptionsCallback(IOException exception, int retryCount, int maxRetryCount, int waitTime);

    public static void WrapSharingViolations(Action action) => WrapSharingViolations(action, DefaultWrapSharingViolationsRetryCount, DefaultWrapSharingViolationsWaitTime);
    public static void WrapSharingViolations(Action action, int maxRetryCount, int waitTime) => WrapSharingViolations(action, null, maxRetryCount, waitTime);
    public static void WrapSharingViolations(Action action, WrapSharingViolationsExceptionsCallback exceptionsCallback, int maxRetryCount, int waitTime)
    {
        ArgumentNullException.ThrowIfNull(action);

        for (var i = 0; i < maxRetryCount; i++)
        {
            try
            {
                action();
                return;
            }
            catch (IOException ioe)
            {
                if (IsSharingViolation(ioe) && i < (maxRetryCount - 1))
                {
                    bool wait = true;
                    if (exceptionsCallback != null)
                    {
                        wait = exceptionsCallback(ioe, i, maxRetryCount, waitTime);
                    }

                    if (wait)
                    {
                        Thread.Sleep(waitTime);
                    }
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public static T WrapSharingViolations<T>(Func<T> func) => WrapSharingViolations(func, DefaultWrapSharingViolationsRetryCount, DefaultWrapSharingViolationsWaitTime);
    public static T WrapSharingViolations<T>(Func<T> func, int maxRetryCount, int waitTime) => WrapSharingViolations(func, null, maxRetryCount, waitTime);
    public static T WrapSharingViolations<T>(Func<T> func, WrapSharingViolationsExceptionsCallback exceptionsCallback, int maxRetryCount, int waitTime)
    {
        ArgumentNullException.ThrowIfNull(func);

        for (var i = 0; i < maxRetryCount; i++)
        {
            try
            {
                return func();
            }
            catch (IOException ioe)
            {
                if (IsSharingViolation(ioe) && i < (maxRetryCount - 1))
                {
                    bool wait = true;
                    if (exceptionsCallback != null)
                    {
                        wait = exceptionsCallback(ioe, i, maxRetryCount, waitTime);
                    }

                    if (wait)
                    {
                        Thread.Sleep(waitTime);
                    }
                }
                else
                {
                    throw;
                }
            }
        }
        return default;
    }

    public static bool IsSharingViolation(IOException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
        return exception.HResult == ERROR_SHARING_VIOLATION;
    }
}
