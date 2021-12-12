using Microsoft.Toolkit.Uwp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.UI.Core;

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
            
            await Common.Extensions.IgnoreExceptions(async () =>
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
