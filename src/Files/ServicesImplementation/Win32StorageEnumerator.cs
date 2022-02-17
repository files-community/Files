using System;
using System.Collections.Generic;
using System.IO;
using Files.Backend.Services;
using Files.Backend.ViewModels.ItemListing;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.ServicesImplementation
{
    internal sealed class Win32StorageEnumerator : IStorageEnumeratorService
    {
        private IntPtr hFile;
        private WIN32_FIND_DATA findData;

        public string WorkingDirectory
        {
            get; private set;
        }

        public bool IsSupported()
        {
            if (string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                return false;
            }

            if (hFile == null)
            {
                FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
                int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
                hFile = FindFirstFileExFromApp(WorkingDirectory + "\\*.*", findInfoLevel, out findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                    additionalFlags);
            }
            return !(hFile == IntPtr.Zero || hFile.ToInt64() == -1);
        }

        public void SetWorkingDirectory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                WorkingDirectory = null;
                return;
            }

            if (App.LibraryManager.TryGetLibrary(value, out _) 
                || !Path.IsPathRooted(value) || value == "Home".GetLocalized())
            {
                WorkingDirectory = null;
            }
            else
            {
                WorkingDirectory = value;
            }
        }

        public IEnumerable<ListedItemViewModel> Enumerate()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
