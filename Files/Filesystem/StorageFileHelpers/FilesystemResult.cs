using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem
{
    [Flags]
    public enum FilesystemErrorCode
    {
        ERROR_SUCCESS = 0,
        ERROR_GENERIC = 1,
        ERROR_UNAUTHORIZED = 2,
        ERROR_NOTFOUND = 4,
        ERROR_INUSE = 8,
        ERROR_NAMETOOLONG = 16,
        ERROR_ALREADYEXIST = 32,
        ERROR_NOTAFOLDER = 64,
        ERROR_NOTAFILE = 128,
        ERROR_INPROGRESS = 256
    }

    public static class FilesystemErrorCodeExtensions
    {
        public static bool HasFlag(this FilesystemErrorCode errorCode, FilesystemErrorCode flag)
        {
            return (errorCode & flag) != FilesystemErrorCode.ERROR_SUCCESS;
        }
    }

    public class FilesystemResult<T> : FilesystemResult
    {
        public FilesystemResult(T result, FilesystemErrorCode errorCode) : base(errorCode)
        {
            this.Result = result;
        }

        public T Result { get; private set; }

        public static implicit operator T(FilesystemResult<T> res) => res.Result;
    }

    public class FilesystemResult
    {
        public FilesystemResult(FilesystemErrorCode errorCode)
        {
            this.ErrorCode = errorCode;
        }

        public FilesystemErrorCode ErrorCode { get; private set; }

        public static implicit operator FilesystemErrorCode(FilesystemResult res) => res.ErrorCode;

        public static implicit operator FilesystemResult(FilesystemErrorCode res) => new FilesystemResult(res);

        public static implicit operator bool(FilesystemResult res) =>
            res.ErrorCode == FilesystemErrorCode.ERROR_SUCCESS;

        public static explicit operator FilesystemResult(bool res) =>
            new FilesystemResult(res ? FilesystemErrorCode.ERROR_SUCCESS : FilesystemErrorCode.ERROR_GENERIC);
    }

    public static class FilesystemTasks
    {
        public static FilesystemErrorCode GetErrorCode(Exception ex, Type T = null)
        {
            if (ex is UnauthorizedAccessException)
            {
                return FilesystemErrorCode.ERROR_UNAUTHORIZED;
            }
            else if (ex is FileNotFoundException // Item was deleted
                || ex is System.Runtime.InteropServices.COMException // Item's drive was ejected
                || (uint)ex.HResult == 0x8007000F) // The system cannot find the drive specified
            {
                return FilesystemErrorCode.ERROR_NOTFOUND;
            }
            else if (ex is IOException || ex is FileLoadException)
            {
                return FilesystemErrorCode.ERROR_INUSE;
            }
            else if (ex is PathTooLongException)
            {
                return FilesystemErrorCode.ERROR_NAMETOOLONG;
            }
            else if (ex is ArgumentException) // Item was invalid
            {
                return (T == typeof(StorageFolder) || T == typeof(StorageFolderWithPath)) ?
                    FilesystemErrorCode.ERROR_NOTAFOLDER : FilesystemErrorCode.ERROR_NOTAFILE;
            }
            else if ((uint)ex.HResult == 0x800700B7)
            {
                return FilesystemErrorCode.ERROR_ALREADYEXIST;
            }
            else if ((uint)ex.HResult == 0x800700A1 // The specified path is invalid (usually an mtp device was disconnected)
                || (uint)ex.HResult == 0x8007016A // The cloud file provider is not running
                || (uint)ex.HResult == 0x8000000A) // The data necessary to complete this operation is not yet available)
            {
                return FilesystemErrorCode.ERROR_GENERIC;
            }
            else
            {
                return FilesystemErrorCode.ERROR_GENERIC;
            }
        }

        public async static Task<FilesystemResult<T>> Wrap<T>(Func<Task<T>> wrapped)
        {
            try
            {
                return new FilesystemResult<T>(await wrapped(), FilesystemErrorCode.ERROR_SUCCESS);
            }
            catch (Exception ex)
            {
                return new FilesystemResult<T>(default, GetErrorCode(ex, typeof(T)));
            }
        }

        public async static Task<FilesystemResult> Wrap(Func<Task> wrapped)
        {
            try
            {
                await wrapped();
                return new FilesystemResult(FilesystemErrorCode.ERROR_SUCCESS);
            }
            catch (Exception ex)
            {
                return new FilesystemResult(GetErrorCode(ex));
            }
        }

        public async static Task<FilesystemResult<T>> OnSuccess<T>(this Task<FilesystemResult<T>> wrapped, Func<T, Task<T>> func)
        {
            var res = await wrapped;
            if (res)
            {
                return await Wrap(() => func(res.Result));
            }
            return res;
        }

        public async static Task<FilesystemResult> OnSuccess<T>(this Task<FilesystemResult<T>> wrapped, Func<T, Task> func)
        {
            var res = await wrapped;
            if (res)
            {
                return await Wrap(() => func(res.Result));
            }
            return res;
        }

        public async static Task<FilesystemResult> OnSuccess<T>(this Task<FilesystemResult<T>> wrapped, Action<T> func)
        {
            var res = await wrapped;
            if (res)
            {
                func(res.Result);
            }
            return res;
        }
    }
}