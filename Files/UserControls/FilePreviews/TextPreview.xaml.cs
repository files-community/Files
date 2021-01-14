using Files.Filesystem;
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
        public TextPreview(string path)
        {
            this.InitializeComponent();
            SetFile(path);
        }

        public string TextValue
        {
            get => Text.Text;
            set => Text.Text = value;
        }

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
                    TextValue = text
                };
            } catch
            {
                return null;
            }
        }

        private async void SetFile(string path)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var text = await FileIO.ReadTextAsync(file);
            Text.Text = text;
        }
    }
}
