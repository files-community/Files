using Files.Common;
using System;
using System.IO;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Storage;

namespace FilesFullTrust.Helpers
{
    public class FileTagsHelper
    {
        private static string ReadFileTag(string filePath)
        {
            using var hStream = Kernel32.CreateFile($"{filePath}:files",
                Kernel32.FileAccess.GENERIC_READ, 0, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid) return null;
            var bytes = new byte[4096];
            var ret = Kernel32.ReadFile(hStream, bytes, (uint)bytes.Length, out var read, IntPtr.Zero);
            if (!ret) return null;
            return System.Text.Encoding.UTF8.GetString(bytes, 0, (int)read);
        }

        public static void UpdateTagsDb()
        {
            string FileTagsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");
            using var dbInstance = new Common.FileTagsDb(FileTagsDbPath, true);
            foreach (var file in dbInstance.GetAll())
            {
                var pathFromFrn = Win32API.PathFromFileId(file.Frn ?? 0, file.FilePath);
                if (pathFromFrn != null)
                {
                    // Frn is valid, update file path
                    var tag = ReadFileTag(pathFromFrn.Replace(@"\\?\", ""));
                    if (tag != null)
                    {
                        dbInstance.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", ""));
                        dbInstance.SetTag(pathFromFrn.Replace(@"\\?\", ""), file.Frn, tag);
                    }
                    else
                    {
                        dbInstance.SetTag(null, file.Frn, null);
                    }
                }
                else
                {
                    var tag = ReadFileTag(file.FilePath);
                    if (tag != null)
                    {
                        if (!Extensions.IgnoreExceptions(() =>
                        {
                            using var si = new ShellItem(file.FilePath);
                            var frn = si.Properties["System.FileFRN"];
                            dbInstance.UpdateTag(file.FilePath, (ulong)frn, null);
                            dbInstance.SetTag(file.FilePath, (ulong)frn, tag);
                        }))
                        {
                            dbInstance.SetTag(file.FilePath, null, null);
                        }
                    }
                    else
                    {
                        dbInstance.SetTag(file.FilePath, null, null);
                    }
                }
            }
        }
    }
}
