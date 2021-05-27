using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Views.LayoutModes
{
    interface ILayoutMode
    {
        public BaseLayoutViewModel ViewModel { get; }
    }
}
