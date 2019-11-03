using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public class SidebarItem
    {
        public string IconGlyph { get; set; }

        public string Text { get; set; }

        public bool isDefaultLocation { get; set; } = false;

        public string Path { get; set; } = null;
    }
}
