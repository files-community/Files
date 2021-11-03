using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Threading.Tasks;

namespace Files.Views
{
    public sealed partial class PropertiesCompatibility : PropertiesTab
    {
        public PropertiesCompatibility()
        {
            this.InitializeComponent();
        }

        public override async Task<bool> SaveChangesAsync(ListedItem item)
        {
            return await Task.FromResult(true);
        }

        public override void Dispose()
        {
        }
    }
}
