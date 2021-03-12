using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Interacts
{
    public class BaseLayoutCommandImplementationModel : IBaseLayoutCommandImplementationModel
    {
        #region Private Members

        private readonly IShellPage associatedInstance;

        #endregion

        #region Constructor

        public BaseLayoutCommandImplementationModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
        }

        #endregion

        #region Command Implementation

        // Here will reside all commands implementation

        public void Example(EventArgs e)
        {
            // Example implementation here :)
        }

        #endregion
    }
}
