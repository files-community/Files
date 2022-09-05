using Files.Shared.Extensions;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Files.App.Helpers
{
    public static class QuickLookHelpers
    {
        public static async Task ToggleQuickLook(IShellPage associatedInstance, bool switchPreview = false)
        {
            if (!App.AppModel.IsQuickLookSupported || !associatedInstance.SlimContentPage.IsItemSelected || associatedInstance.SlimContentPage.IsRenamingItem)
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
