using System.Threading.Tasks;
using Files.Filesystem;
using Files.ViewModels.Properties;

namespace Files.Views
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