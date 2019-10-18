using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
    public class DriveItem
    {
        public string glyph { get; set; }
        public ulong maxSpace { get; set; }
        public ulong spaceUsed { get; set; }
        public string driveText { get; set; }
        public string tag { get; set; }
        public Visibility progressBarVisibility { get; set; }
        public string spaceText { get; set; }
    }
}
