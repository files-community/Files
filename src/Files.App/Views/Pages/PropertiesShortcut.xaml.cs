using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using System.Threading.Tasks;

namespace Files.App.Views
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