using System;
using System.IO;

namespace Files.Common
{
    public class ShellLibraryItem
    {
        public const string EXTENSION = ".library-ms";

        public static readonly string LibrariesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Libraries");

        /// <summary>
        /// Full path of library file.<br/>
        /// <br/>
        /// C:\Users\[username]\AppData\Roaming\Microsoft\Windows\Libraries\Documents.library-ms<br/>
        /// C:\Users\[username]\AppData\Roaming\Microsoft\Windows\Libraries\Custom library.library-ms
        /// </summary>
        public string FullPath;

        /// <summary>
        /// ShellItemDisplayString.DesktopAbsoluteParsing<br/>
        /// <br/>
        /// ::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\Documents.library-ms<br/>
        /// C:\Users\[username]\AppData\Roaming\Microsoft\Windows\Libraries\Custom library.library-ms
        /// </summary>
        public string AbsolutePath;

        /// <summary>
        /// ShellItemDisplayString.ParentRelativeParsing<br/>
        /// <br/>
        /// {7B0DB17D-9CD2-4A93-9733-46CC89022E7C}<br/>
        /// Custom library.library-ms
        /// </summary>
        public string RelativePath;

        /// <summary>
        /// ShellItemDisplayString.NormalDisplay<br/>
        /// <br/>
        /// Documents (locale dependent based on desktop.ini file of the Libraries folder)<br/>
        /// Custom library (locale independent)
        /// </summary>
        public string DisplayName;

        public bool IsPinned;
        public string DefaultSaveFolder;
        public string[] Folders;

        public ShellLibraryItem()
        {
        }
    }
}