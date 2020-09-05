using Files.Filesystem;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

namespace Files
{
    public sealed partial class PropertiesGeneral : PropertiesTab
    {
        public PropertiesGeneral()
        {
            this.InitializeComponent();
            base.ItemMD5HashProgress = ItemMD5HashProgress;
        }

        public async Task SaveChanges(ListedItem item)
        {
            if (ViewModel.OriginalItemName != null)
            {
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => App.CurrentInstance.InteractionOperations.RenameFileItem(item,
                      ViewModel.OriginalItemName,
                      ViewModel.ItemName));
            }
        }
    }
}