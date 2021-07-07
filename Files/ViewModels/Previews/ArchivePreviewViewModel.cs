﻿using Files.Filesystem;
using Files.ViewModels.Properties;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Files.Extensions;
using ByteSizeLib;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp;

namespace Files.ViewModels.Previews
{
    public class ArchivePreviewViewModel : BasePreviewModel
    {
        public static List<string> Extensions => new List<string>()
        {
            ".zip",
        };

        public ArchivePreviewViewModel(ListedItem item) : base(item)
        {
        }

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var details = new List<FileProperty>();
            using ZipFile zipFile = new ZipFile(await Item.ItemFile.OpenStreamForReadAsync());
            zipFile.IsStreamOwner = true;

            var folderCount = 0;
            var fileCount = 0;
            long totalSize = 0;

            foreach (ZipEntry entry in zipFile)
            {
                if (entry.IsFile)
                {
                    fileCount++;
                    totalSize += entry.Size;
                }
                else
                {
                    folderCount++;
                }
            }

            details.Add(new FileProperty()
            {
                NameResource = "PropertyItemCount",
                Value = string.Format("DetailsArchiveItemCount".GetLocalized(), zipFile.Count, fileCount, folderCount),
            });
            
            details.Add(new FileProperty()
            {
                NameResource = "PropertyUncompressedSize",
                Value = $"{ByteSize.FromBytes(totalSize).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(totalSize).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})",
            });

            _ = await base.LoadPreviewAndDetails(); // Loads the thumbnail preview
            return details;
        }
    }
}
