using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Threading.Tasks;

namespace Files.Views
{
    public sealed partial class PropertiesShortcut : PropertiesTab
    {
        public PropertiesShortcut()
        {
            this.InitializeComponent();
        }

        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            return false;
        }
        public override void Dispose()
        {
        }
    }
}