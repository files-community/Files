using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Files.Backend.Item
{
    public class ItemException : Exception
    {
        public ItemErrors Error { get; } = ItemErrors.Unknown;

        internal ItemException() {}
        internal ItemException(string message) : base(message) {}
        internal ItemException(string message, Exception innerException) : base(message, innerException)
            => Error = GetError(innerException);

        private static ItemErrors GetError(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => ItemErrors.Unauthorized,
                FileNotFoundException => ItemErrors.NotFound, // Item was deleted
                COMException => ItemErrors.NotFound, // Item's drive was ejected
                _ when (uint)ex.HResult == 0x8007000F => ItemErrors.NotFound, // The system cannot find the drive specified
                PathTooLongException => ItemErrors.NameTooLong,
                IOException => ItemErrors.InUse,
                ArgumentException => ItemErrors.Unknown, // Item was invalid
                _ => ItemErrors.Unknown,
            };
        }
    }
}
