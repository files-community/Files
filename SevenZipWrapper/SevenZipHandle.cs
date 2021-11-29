using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    internal class SevenZipHandle : IDisposable
    {
        private SafeLibraryHandle sevenZipSafeHandle;

        public SevenZipHandle(string sevenZipLibPath)
        {
            this.sevenZipSafeHandle = Kernel32Dll.LoadPackagedLibrary(sevenZipLibPath);

            if (this.sevenZipSafeHandle.IsInvalid)
            {
                throw new Win32Exception();
            }

            IntPtr functionPtr = Kernel32Dll.GetProcAddress(this.sevenZipSafeHandle, "GetHandlerProperty");
            
            // Not valid dll
            if (functionPtr == IntPtr.Zero)
            {
                this.sevenZipSafeHandle.Close();
                throw new ArgumentException();
            }
        }

        ~SevenZipHandle()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if ((this.sevenZipSafeHandle != null) && !this.sevenZipSafeHandle.IsClosed)
            {
                this.sevenZipSafeHandle.Close();
            }

            this.sevenZipSafeHandle = null;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IInArchive CreateInArchive(Guid classId)
        {
            if (this.sevenZipSafeHandle == null)
            {
                throw new ObjectDisposedException("SevenZipHandle");
            }

            IntPtr procAddress = Kernel32Dll.GetProcAddress(this.sevenZipSafeHandle, "CreateObject");
            CreateObjectDelegate createObject = (CreateObjectDelegate) Marshal.GetDelegateForFunctionPointer(procAddress, typeof (CreateObjectDelegate));

            object result;
            Guid interfaceId = typeof (IInArchive).GUID;
            createObject(ref classId, ref interfaceId, out result);

            return result as IInArchive;
        }
    }
}