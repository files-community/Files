using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Threading.Tasks;

namespace Files.Views
{
    public sealed partial class PropertiesShortcut : PropertiesTab
    {
        public PropertiesShortcut()
        {
            InitializeComponent();
        }

        #pragma warning disable 1998
        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            return false;
        }
        #pragma warning restore 1998

        public override void Dispose()
        {
        }
    }
}