using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Filesystem.Security
{
    public class Permission
    {
        public int Id { get; set; }
        public string Descripton { get; set; }
        public bool Allow { get; set; }
        public bool Deny { get; set; }
    }
}
