using Files.Filesystem;
using Files.Filesystem.Permissions;
using Files.ViewModels.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab, INotifyPropertyChanged
    {
        public PropertiesSecurity()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            if (BaseProperties is FileProperties fileProps)
            {
                return await fileProps.SetFilePermissionProperties();
            }
            else if (BaseProperties is FolderProperties folderProps)
            {
                return await folderProps.SetFolderPermissionProperties();
            }
            return false;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties is FileProperties fileProps)
            {
                fileProps.GetFilePermissionProperties();
            }
            else if (BaseProperties is FolderProperties folderProps)
            {
                folderProps.GetFolderPermissionProperties();
            }
        }

        public override void Dispose()
        {
        }

        private RulesForUser selectedAccessRule;
        public RulesForUser SelectedAccessRule
        {
            get => selectedAccessRule;
            set
            {
                if (value != selectedAccessRule)
                {
                    selectedAccessRule = value;
                    NotifyPropertyChanged(nameof(SelectedAccessRule));
                }
            }
        }
    }
}
