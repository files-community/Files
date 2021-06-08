using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public PropertiesSecurity()
        {
            this.InitializeComponent();
        }

        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            if (BaseProperties is FileSystemProperties fileSysProps)
            {
                return await fileSysProps.SetFilePermissions();
            }
            return true;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties is FileSystemProperties fileSysProps)
            {
                fileSysProps.GetFilePermissions();
            }
        }

        public override void Dispose()
        {
        }
    }
}
