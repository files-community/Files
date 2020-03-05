using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.UserControls.Ribbon
{
    public interface IRibbonItem
    {
        public RibbonItemDisplayMode DisplayMode { get; set; }
        public string TooltipText { get; set; }
        public string LabelText { get; set; }
        public double EstimatedWidth { get; }
    }

    public enum RibbonItemDisplayMode
    {
        Divider,
        Compact,
        Wide,
        Tall,
    }
}
