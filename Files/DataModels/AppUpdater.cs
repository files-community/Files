using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Files.DataModels
{
    class AppUpdater
    {
        private StoreContext context = null;
        private IReadOnlyList<StorePackageUpdate> UpdateList = null;

        public AppUpdater()
        {
            context = StoreContext.GetDefault();
        }

        public async Task<int> CheckForUpdatesAsync(bool mandantoryOnly = false)
        {
            if (context == null)
            {
                context = StoreContext.GetDefault();
            }

            UpdateList = await context.GetAppAndOptionalStorePackageUpdatesAsync();

            if (mandantoryOnly)
            {
                UpdateList = (IReadOnlyList<StorePackageUpdate>)UpdateList.Where(e => e.Mandatory);
            }

            return UpdateList.Count;
        }
    }
}
