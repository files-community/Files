using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Commands
{
    public partial class ItemOperations
    {
        IShellPage AppInstance = null;
        public ItemOperations(IShellPage appInstance)
        {
            AppInstance = appInstance;
        }
    }
}
