#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// Zip encryption method enum.
    /// </summary>
    public enum ZipEncryptionMethod
    {
        /// <summary>
        /// ZipCrypto encryption method.
        /// </summary>
        ZipCrypto,
        /// <summary>
        /// AES 128 bit encryption method.
        /// </summary>
        Aes128,
        /// <summary>
        /// AES 192 bit encryption method.
        /// </summary>
        Aes192,
        /// <summary>
        /// AES 256 bit encryption method.
        /// </summary>
        Aes256
    }
}

#endif
