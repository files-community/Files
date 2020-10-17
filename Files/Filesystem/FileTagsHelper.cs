using Common;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public class FileTagsHelper
    {
        public static string FileTagsDbPath => System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

        private static FileTagsDb _DbInstance;
        public static FileTagsDb DbInstance
        {
            get
            {
                if (_DbInstance == null)
                {
                    _DbInstance = new FileTagsDb(FileTagsDbPath);
                }
                return _DbInstance;
            }
        }

        private FileTagsHelper()
        {

        }
    }

    public class FileTag
    {
        public string Tag { get; set; }
        public SolidColorBrush Color { get; set; }

        public FileTag(string tag = null, SolidColorBrush color = null)
        {
            Tag = tag;
            Color = color ?? new SolidColorBrush(Colors.Transparent);
        }

        public FileTag(string tag = null, Color? color = null)
        {
            Tag = tag;
            Color = new SolidColorBrush(color ?? Colors.Transparent);
        }

        public FileTag(string tag = null, string color = null)
        {
            Tag = tag;
            Color = new SolidColorBrush(color?.ToColor() ?? Colors.Transparent);
        }
    }
}
