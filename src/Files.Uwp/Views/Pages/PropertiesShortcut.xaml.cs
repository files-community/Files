using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using System.Threading.Tasks;

namespace Files.Uwp.Views
{
    public sealed partial class PropertiesShortcut : PropertiesTab
    {
        public PropertiesShortcut()
        {
            InitializeComponent();
        }

        public override Task<bool> SaveChangesAsync(ListedItem item)
        {
            return Task.FromResult(true);
        }

        public override void Dispose()
        {
        }
    }
}