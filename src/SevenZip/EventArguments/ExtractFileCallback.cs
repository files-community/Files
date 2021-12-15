#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// Callback delegate for <see cref="SevenZipExtractor.ExtractFiles(SevenZip.ExtractFileCallback)"/>.
    /// </summary>
    public delegate void ExtractFileCallback(ExtractFileCallbackArgs extractFileCallbackArgs);
}

#endif
