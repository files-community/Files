// Version 1.5

using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace SevenZipExtractor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropArray
    {
        uint length;
        IntPtr pointerValues;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariant
    {
        [DllImport("api-ms-win-core-com-l1-1-1.dll")]
        private static extern int PropVariantClear(ref PropVariant pvar);

        [DllImport("propsys.dll")]
        private static extern int PropVariantToWinRTPropertyValue(ref PropVariant pvar, Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        [DllImport("propsys.dll")]
        private static extern string PropVariantToStringWithDefault(ref PropVariant propvarIn, [In, MarshalAs(UnmanagedType.LPWStr)] string pszDefault);

        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pointerValue;
        [FieldOffset(8)] public byte byteValue;
        [FieldOffset(8)] public long longValue;
        [FieldOffset(8)] public System.Runtime.InteropServices.ComTypes.FILETIME filetime;
        [FieldOffset(8)] public PropArray propArray;

        public VarEnum VarType
        {
            get
            {
                return (VarEnum) this.vt;
            }
        }

        public void Clear()
        {
            switch (this.VarType)
            {
                case VarEnum.VT_EMPTY:
                    break;

                case VarEnum.VT_NULL:
                case VarEnum.VT_I2:
                case VarEnum.VT_I4:
                case VarEnum.VT_R4:
                case VarEnum.VT_R8:
                case VarEnum.VT_CY:
                case VarEnum.VT_DATE:
                case VarEnum.VT_ERROR:
                case VarEnum.VT_BOOL:
                //case VarEnum.VT_DECIMAL:
                case VarEnum.VT_I1:
                case VarEnum.VT_UI1:
                case VarEnum.VT_UI2:
                case VarEnum.VT_UI4:
                case VarEnum.VT_I8:
                case VarEnum.VT_UI8:
                case VarEnum.VT_INT:
                case VarEnum.VT_UINT:
                case VarEnum.VT_HRESULT:
                case VarEnum.VT_FILETIME:
                    this.vt = 0;
                    break;

                default:
                    PropVariantClear(ref this);
                    break;
            }
        }

        public object GetObject()
        {
            switch (this.VarType)
            {
                case VarEnum.VT_EMPTY:
                    return null;

                case VarEnum.VT_FILETIME:
                    return DateTime.FromFileTime(this.longValue);

                //case VarEnum.VT_BSTR:
                    //return PropVariantToStringWithDefault(ref this, null);

                default:
                    PropVariantToWinRTPropertyValue(ref this, typeof(PropertyValue).GUID, out var ppv);
                    return null;
            }
        }
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000000050000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IProgress
    {
        void SetTotal(ulong total);
        void SetCompleted([In] ref ulong completeValue);
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000600100000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IArchiveOpenCallback
    {
        // ref ulong replaced with IntPtr because handlers ofter pass null value
        // read actual value with Marshal.ReadInt64
        void SetTotal(
            IntPtr files, // [In] ref ulong files, can use 'ulong* files' but it is unsafe
            IntPtr bytes); // [In] ref ulong bytes

        void SetCompleted(
            IntPtr files, // [In] ref ulong files
            IntPtr bytes); // [In] ref ulong bytes
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000500100000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICryptoGetTextPassword
    {
        [PreserveSig]
        int CryptoGetTextPassword(
            [MarshalAs(UnmanagedType.BStr)] out string password);

        //[return : MarshalAs(UnmanagedType.BStr)]
        //string CryptoGetTextPassword();
    }

    internal enum AskMode : int
    {
        kExtract = 0,
        kTest,
        kSkip
    }

    internal enum OperationResult : int
    {
        kOK = 0,
        kUnSupportedMethod,
        kDataError,
        kCRCError
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000600300000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IArchiveOpenVolumeCallback
    {
        void GetProperty(
            ItemPropId propID, // PROPID
            IntPtr value); // PROPVARIANT

        [PreserveSig]
        int GetStream(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.Interface)] out IInStream inStream);
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000600400000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IInArchiveGetStream
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        ISequentialInStream GetStream(uint index);
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000300010000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISequentialInStream
    {
        //[PreserveSig]
        //int Read(
        //  [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
        //  uint size,
        //  IntPtr processedSize); // ref uint processedSize

        uint Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size);

        /*
    Out: if size != 0, return_value = S_OK and (*processedSize == 0),
      then there are no more bytes in stream.
    if (size > 0) && there are bytes in stream, 
    this function must read at least 1 byte.
    This function is allowed to read less than number of remaining bytes in stream.
    You must call Read function in loop, if you need exact amount of data
    */
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000300020000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISequentialOutStream
    {
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size,
            IntPtr processedSize); // ref uint processedSize
        /*
    if (size > 0) this function must write at least 1 byte.
    This function is allowed to write less than "size".
    You must call Write function in loop, if you need to write exact amount of data
    */
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000300030000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IInStream //: ISequentialInStream
    {
        //[PreserveSig]
        //int Read(
        //  [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
        //  uint size,
        //  IntPtr processedSize); // ref uint processedSize

        uint Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size);

        //[PreserveSig]
        void Seek(
            long offset,
            uint seekOrigin,
            IntPtr newPosition); // ref long newPosition
    }

    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000300040000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOutStream //: ISequentialOutStream
    {
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size,
            IntPtr processedSize); // ref uint processedSize

        //[PreserveSig]
        void Seek(
            long offset,
            uint seekOrigin,
            IntPtr newPosition); // ref long newPosition

        [PreserveSig]
        int SetSize(long newSize);
    }

    internal enum ItemPropId : uint
    {
        kpidNoProperty = 0,

        kpidHandlerItemIndex = 2,
        kpidPath,
        kpidName,
        kpidExtension,
        kpidIsFolder,
        kpidSize,
        kpidPackedSize,
        kpidAttributes,
        kpidCreationTime,
        kpidLastAccessTime,
        kpidLastWriteTime,
        kpidSolid,
        kpidCommented,
        kpidEncrypted,
        kpidSplitBefore,
        kpidSplitAfter,
        kpidDictionarySize,
        kpidCRC,
        kpidType,
        kpidIsAnti,
        kpidMethod,
        kpidHostOS,
        kpidFileSystem,
        kpidUser,
        kpidGroup,
        kpidBlock,
        kpidComment,
        kpidPosition,
        kpidPrefix,

        kpidTotalSize = 0x1100,
        kpidFreeSpace,
        kpidClusterSize,
        kpidVolumeName,

        kpidLocalName = 0x1200,
        kpidProvider,

        kpidUserDefined = 0x10000
    }


    [ComImport]
    [Guid("23170F69-40C1-278A-0000-000600600000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //[AutomationProxy(true)]
    internal interface IInArchive
    {
        [PreserveSig]
        int Open(
            IInStream stream,
            /*[MarshalAs(UnmanagedType.U8)]*/ [In] ref ulong maxCheckStartPosition,
            [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback openArchiveCallback);

        void Close();
        //void GetNumberOfItems([In] ref uint numItem);
        uint GetNumberOfItems();

        void GetProperty(
            uint index,
            ItemPropId propID, // PROPID
            ref PropVariant value); // PROPVARIANT

        [PreserveSig]
        int Extract(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] indices, //[In] ref uint indices,
            uint numItems,
            int testMode,
            [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);

        // indices must be sorted 
        // numItems = 0xFFFFFFFF means all files
        // testMode != 0 means "test files operation"

        void GetArchiveProperty(
            uint propID, // PROPID
            ref PropVariant value); // PROPVARIANT

        //void GetNumberOfProperties([In] ref uint numProperties);
        uint GetNumberOfProperties();

        void GetPropertyInfo(
            uint index,
            [MarshalAs(UnmanagedType.BStr)] out string name,
            out ItemPropId propID, // PROPID
            out ushort varType); //VARTYPE

        //void GetNumberOfArchiveProperties([In] ref uint numProperties);
        uint GetNumberOfArchiveProperties();

        void GetArchivePropertyInfo(
            uint index,
            [MarshalAs(UnmanagedType.BStr)] string name,
            ref uint propID, // PROPID
            ref ushort varType); //VARTYPE
    }

    internal enum ArchivePropId : uint
    {
        kName = 0,
        kClassID,
        kExtension,
        kAddExtension,
        kUpdate,
        kKeepName,
        kStartSignature,
        kFinishSignature,
        kAssociate
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int CreateObjectDelegate(
        [In] ref Guid classID,
        [In] ref Guid interfaceID,
        //out IntPtr outObject);
        [MarshalAs(UnmanagedType.Interface)] out object outObject);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int GetHandlerPropertyDelegate(
        ArchivePropId propID,
        ref PropVariant value); // PROPVARIANT

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int GetNumberOfFormatsDelegate(out uint numFormats);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int GetHandlerProperty2Delegate(
        uint formatIndex,
        ArchivePropId propID,
        ref PropVariant value); // PROPVARIANT

    internal class StreamWrapper : IDisposable
    {
        protected Stream BaseStream;

        protected StreamWrapper(Stream baseStream)
        {
            this.BaseStream = baseStream;
        }

        public void Dispose()
        {
            this.BaseStream.Close();
        }

        public virtual void Seek(long offset, uint seekOrigin, IntPtr newPosition)
        {
            long Position = (uint) this.BaseStream.Seek(offset, (SeekOrigin) seekOrigin);

            if (newPosition != IntPtr.Zero)
            {
                Marshal.WriteInt64(newPosition, Position);
            }
        }
    }

    internal class InStreamWrapper : StreamWrapper, ISequentialInStream, IInStream
    {
        public InStreamWrapper(Stream baseStream) : base(baseStream)
        {
        }

        public uint Read(byte[] data, uint size)
        {
            return (uint) this.BaseStream.Read(data, 0, (int) size);
        }
    }

    internal class OutStreamWrapper : StreamWrapper, ISequentialOutStream, IOutStream
    {
        public OutStreamWrapper(Stream baseStream) : base(baseStream)
        {
        }

        public int SetSize(long newSize)
        {
            this.BaseStream.SetLength(newSize);
            return 0;
        }

        public int Write(byte[] data, uint size, IntPtr processedSize)
        {
            this.BaseStream.Write(data, 0, (int) size);

            if (processedSize != IntPtr.Zero)
            {
                Marshal.WriteInt32(processedSize, (int) size);
            }

            return 0;
        }
    }
}