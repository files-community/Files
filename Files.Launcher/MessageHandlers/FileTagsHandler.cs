using Files.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Storage;

namespace FilesFullTrust.MessageHandlers
{
    public class FileTagsHandler : IMessageHandler
    {
        private string ReadFileTag(string filePath)
        {
            using var hStream = Kernel32.CreateFile($"{filePath}:files",
                Kernel32.FileAccess.GENERIC_READ, 0, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid) return null;
            var bytes = new byte[4096];
            var ret = Kernel32.ReadFile(hStream, bytes, (uint)bytes.Length, out var read, IntPtr.Zero);
            if (!ret) return null;
            return System.Text.Encoding.UTF8.GetString(bytes, 0, (int)read);
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
                            var frn = (ulong?)si.Properties["System.FileFRN"];
                            dbInstance.UpdateTag(file.FilePath, frn, null);
                            dbInstance.SetTag(file.FilePath, (ulong?)frn, tag);
                        }, Program.Logger))
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

        public Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "UpdateTagsDb":
                    UpdateTagsDb();
                    break;
            }
            return Task.CompletedTask;
        }

        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public void Dispose()
        {
        }
    }
}
