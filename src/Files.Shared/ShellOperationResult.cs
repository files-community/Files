using System.Collections.Generic;
using System.Linq;

namespace Files.Shared
{
    public class ShellOperationResult
    {
        public ShellOperationResult()
        {
            Items = new List<ShellOperationItemResult>();
        }

        public List<ShellOperationItemResult> Items { get; set; }
        public bool Succeeded { get; set; }

        public List<ShellOperationItemResult> Final =>
            Items.GroupBy(x => new { Src = x.Source, Dst = x.Destination }).Select(x => x.Last()).ToList();
    }

    public class ShellOperationItemResult
    {
        public bool Succeeded { get; set; }
        public int HResult { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
    }
}
