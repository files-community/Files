using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Files.Backend.Services;
using Files.Backend.ViewModels.ItemListing;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp;
using Files.Shared.Extensions;
using static Files.Helpers.NativeFindStorageItemHelper;
using static Files.Helpers.NativeFileOperationsHelper;

#nullable enable

namespace Files.ServicesImplementation
{
    internal sealed class Win32StorageEnumerator : IStorageEnumeratorService
    {
        public bool IsAvailable(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }

            var hFile = FindFirstDefault(directoryPath, out _);

            if (!hFile.IsHandleInvalid())
            {
                // Handle is valid...
                FindClose(hFile);
                return true;
            }

            return false;
        }

        public IEnumerable<string> Enumerate(string path)
        {
            bool hasNext = false;
            var hFile = FindFirstDefault(path, out _);

            if (hFile.IsHandleInvalid())
            {
                return Array.Empty<string>();
            }

            List<string> finalList = new();

            do
            {
                hasNext = FindNextFile(hFile, out var findData);
                finalList.Add(findData.cFileName);
            } while(hasNext);

            FindClose(hFile);

            return finalList;
        }

        private IntPtr FindFirstDefault(string lpFileName, out WIN32_FIND_DATA lpFindFileData)
        {
            return FindFirstFileExFromApp(lpFileName + "\\*.*", FINDEX_INFO_LEVELS.FindExInfoBasic, out lpFindFileData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
        }
    }
}
