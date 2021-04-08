using Files.Filesystem;
using Files.UserControls.FilePreviews;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.ViewModels.Previews
{
    public class TextPreviewViewModel : BasePreviewModel
    {
        private string textValue;

        public TextPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        public static List<string> Extensions => new List<string>() {
            ".txt"
        };

        /// <summary>
        /// A list of extensions that will be ignored when using TryLoadAsTextAsync
        /// </summary>
        public static List<string> ExcludedExtensions => new List<string>()
        {
            ".iso"
        };

        public static async Task<TextPreview> TryLoadAsTextAsync(ListedItem item)
        {
            if (ExcludedExtensions.Contains(item.FileExtension?.ToLower()) || item.FileSizeBytes > Constants.PreviewPane.TryLoadAsTextSizeLimit || item.FileSizeBytes == 0)
            {
                return null;
            }

            try
            {
                item.ItemFile = await StorageFile.GetFileFromPathAsync(item.ItemPath);
                var text = await FileIO.ReadTextAsync(item.ItemFile);

                // Check if file is binary
                if (text.Contains("\0\0\0\0"))
                {
                    return null;
                }

                var model = new TextPreviewViewModel(item)
                {
                    TextValue = text,
                };

                await model.LoadAsync();

                return new TextPreview(model);
            }
            catch
            {
                return null;
            }
        }

        public async override Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var details = new List<FileProperty>();

            try
            {
                var text = TextValue ?? await FileIO.ReadTextAsync(Item.ItemFile);

                details.Add(new FileProperty()
                {
                    NameResource = "PropertyLineCount",
                    Value = text.Split("\n").Length,
                });

                details.Add(new FileProperty()
                {
                    NameResource = "PropertyWordCount",
                    Value = text.Split(new[] { " ", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length,
                });

                var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                TextValue = displayText;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return details;
        }
    }
}