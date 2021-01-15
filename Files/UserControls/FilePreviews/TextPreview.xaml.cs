using Files.Filesystem;
using Files.ViewModels;
using Files.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class TextPreview : UserControl
    {
        public TextPreview(ListedItem item)
        {
            this.InitializeComponent();
            Item = item;
            SetFile(item);
        }

        public string TextValue
        {
            get => Text.Text;
            set
            {
                Text.Text = value;
                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "PropertyLineCount",
                    Value = value.Split("\n").Length,
                });
                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "PropertyWordCount",
                    Value = value.Split(" ").Length,
                });
            }
        }

        private ListedItem Item { get; set; }

        TextPreview()
        {
            this.InitializeComponent();
        }

        public static List<string> Extensions => new List<string>() {
            ".txt"
        };

        public static async Task<TextPreview> TryLoadAsTextAsync(ListedItem item)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
                var text = await FileIO.ReadTextAsync(file);

                // Check if file is binary
                if (text.Contains("\0\0\0\0"))
                {
                    return null;
                }
                return new TextPreview()
                {
                    Item = item,
                    TextValue = text,
                };
            } catch
            {
                return null;
            }
        }

        private async void SetFile(ListedItem item)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            var text = await FileIO.ReadTextAsync(file);
            TextValue = text;
        }
    }
}
