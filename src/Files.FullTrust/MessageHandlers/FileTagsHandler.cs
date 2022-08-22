using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class FileTagsHandler : Disposable, IMessageHandler
    {
        public static string[] ReadFileTag(string filePath)
        {
            using var hStream = Kernel32.CreateFile($"{filePath}:files",
                Kernel32.FileAccess.GENERIC_READ, 0, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid)
            {
                return null;
            }
            var bytes = new byte[4096];
            var ret = Kernel32.ReadFile(hStream, bytes, (uint)bytes.Length, out var read, IntPtr.Zero);
            if (!ret)
            {
                return null;
            }
            var tagString = System.Text.Encoding.UTF8.GetString(bytes, 0, (int)read);
            return tagString.Split(',');
        }

        public static bool WriteFileTag(string filePath, string[] tag)
        {
            var dateOk = GetFileDateModified(filePath, out var dateModified); // Backup date modified
            bool result = false;
            if (tag is null || !tag.Any())
            {
                result = Kernel32.DeleteFile($"{filePath}:files");
            }
            else
            {
                using var hStream = Kernel32.CreateFile($"{filePath}:files",
                    Kernel32.FileAccess.GENERIC_WRITE, 0, null, FileMode.Create, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
                if (hStream.IsInvalid)
                {
                    return false;
                }
                byte[] buff = System.Text.Encoding.UTF8.GetBytes(string.Join(',', tag));
                result = Kernel32.WriteFile(hStream, buff, (uint)buff.Length, out var written, IntPtr.Zero);
            }
            if (dateOk)
            {
                SetFileDateModified(filePath, dateModified); // Restore date modified
            }
            return result;
        }

        public static ulong? GetFileFRN(string filePath)
        {
            //using var si = new ShellItem(filePath);
            //return (ulong?)si.Properties["System.FileFRN"]; // Leaves open file handles
            using var hFile = Kernel32.CreateFile(filePath, Kernel32.FileAccess.GENERIC_READ, FileShare.ReadWrite, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            if (hFile.IsInvalid)
            {
                return null;
            }
            ulong? frn = null;
            SafetyExtensions.IgnoreExceptions(() =>
            {
                var fileID = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(hFile, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
                frn = BitConverter.ToUInt64(fileID.FileId.Identifier, 0);
            });
            return frn;
        }

        public void UpdateTagsDb()
        {
            string fileTagsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");
            using var dbInstance = new Common.FileTagsDb(fileTagsDbPath, true);
            foreach (var file in dbInstance.GetAll())
            {
                var pathFromFrn = Win32API.PathFromFileId(file.Frn ?? 0, file.FilePath);
                if (pathFromFrn != null)
                {
                    // Frn is valid, update file path
                    var tag = ReadFileTag(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
                    if (tag is not null && tag.Any())
                    {
                        dbInstance.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
                        dbInstance.SetTags(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal), file.Frn, tag);
                    }
                    else
                    {
                        dbInstance.SetTags(null, file.Frn, null);
                    }
                }
                else
                {
                    var tag = ReadFileTag(file.FilePath);
                    if (tag is not null && tag.Any())
                    {
                        if (!SafetyExtensions.IgnoreExceptions(() =>
                        {
                            var frn = GetFileFRN(file.FilePath);
                            dbInstance.UpdateTag(file.FilePath, frn, null);
                            dbInstance.SetTags(file.FilePath, (ulong?)frn, tag);
                        }, Program.Logger))
                        {
                            dbInstance.SetTags(file.FilePath, null, null);
                        }
                    }
                    else
                    {
                        dbInstance.SetTags(file.FilePath, null, null);
                    }
                }
            }
        }

        private static bool GetFileDateModified(string filePath, out FILETIME dateModified)
        {
            using var hFile = Kernel32.CreateFile(filePath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            return Kernel32.GetFileTime(hFile, out _, out _, out dateModified);
        }

        private static bool SetFileDateModified(string filePath, FILETIME dateModified)
        {
            using var hFile = Kernel32.CreateFile(filePath, Kernel32.FileAccess.FILE_WRITE_ATTRIBUTES, FileShare.None, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            return Kernel32.SetFileTime(hFile, new(), new(), dateModified);
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "UpdateTagsDb":
                    UpdateTagsDb();
                    break;
            }
            return Task.CompletedTask;
        }

        public void Initialize(PipeStream connection)
        {
        }
    }
}
