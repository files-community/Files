using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000600200000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IArchiveExtractCallback //: IProgress
    {
        void SetTotal(ulong total);
        void SetCompleted([In] ref ulong completeValue);

        [PreserveSig]
        int GetStream(
            uint index,
            [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream outStream,
            AskMode askExtractMode);
        // GetStream OUT: S_OK - OK, S_FALSE - skeep this file

        void PrepareOperation(AskMode askExtractMode);
        void SetOperationResult(OperationResult resultEOperationResult);
    }
}