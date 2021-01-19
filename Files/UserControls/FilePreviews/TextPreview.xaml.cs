using Files.Filesystem;
using Files.ViewModels;
using Files.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.UserControls.FilePreviews
{
    public sealed partial class TextPreview : PreviewControlBase
    {
        public TextPreview(ListedItem item) : base(item)
        {
            this.InitializeComponent();
        }

        public string TextValue
        {
            get => Text.Text;
            set => Text.Text = value;
        }

        public static List<string> Extensions => new List<string>() {
            ".txt"
        };

        /// <summary>
        /// A list of extensions that will be ignored when using TryLoadAsTextAsync
        /// </summary>
        public static List<string> ExcludedExtensions => new List<string>()
        {
            ".iso",
        };

        public static async Task<TextPreview> TryLoadAsTextAsync(ListedItem item)
        {
            if(ExcludedExtensions.Contains(item.FileExtension.ToLower()))
            {
                return null;
            }

            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
                var text = await FileIO.ReadTextAsync(file);

                // Check if file is binary
                if (text.Contains("\0\0\0\0"))
                {
                    return null;
                }
                return new TextPreview(item)
                {
                    ItemFile = file,
                };
            } catch
            {
                return null;
            }
        }

        public async override void LoadPreviewAndDetails()
        {
            try
            {
                var text = await FileIO.ReadTextAsync(ItemFile);

                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "PropertyLineCount",
                    Value = text.Split("\n").Length,
                });
                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "PropertyWordCount",
                    Value = text.Split(" ").Length,
                });

                var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                TextValue = displayText;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            base.LoadSystemFileProperties();
        }
    }
}
