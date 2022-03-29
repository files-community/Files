using System.Diagnostics;
using System.Threading.Tasks;
using Files.Shared.Extensions;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
	public static class QuickLookHelpers
	{
		public static async Task ToggleQuickLook(IShellPage associatedInstance, bool switchPreview = false)
		{
			if (!App.MainViewModel.IsQuickLookEnabled || !associatedInstance.SlimContentPage.IsItemSelected || associatedInstance.SlimContentPage.IsRenamingItem)
			{
				return;
			}

			await SafetyExtensions.IgnoreExceptions(async () =>
			{
				Debug.WriteLine("Toggle QuickLook");
				var connection = await AppServiceConnectionHelper.Instance;

				if (connection != null)
				{
					await connection.SendMessageAsync(new ValueSet()
					{
						{ "path", associatedInstance.SlimContentPage.SelectedItem.ItemPath },
						{ "switch", switchPreview },
						{ "Arguments", "ToggleQuickLook" }
					});
				}

			}, App.Logger);
		}
	}
}
