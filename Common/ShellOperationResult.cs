using System.Collections.Generic;

namespace Files.Common
{
    public class ShellOperationResult
    {
        public ShellOperationResult()
        {
            Items = new List<ShellOperationItemResult>();
        }

        public List<ShellOperationItemResult> Items { get; set; }
        public bool Succeeded { get; set; }
    }

    public class ShellOperationItemResult
    {
        public bool Succeeded { get; set; }
        public int HRresult { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
    }
}
