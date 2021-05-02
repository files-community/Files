using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem.Secutiry
{
    public class UserGroups
    {
        public int Id { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public SecurityType ItemType { get; set; }
    }
    public enum SecurityType { User, Group };
}
