﻿using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.ViewModels.Previews
{
    public class MarkdownPreviewViewModel : BasePreviewModel
    {
        private string textValue;

        public MarkdownPreviewViewModel(ListedItem item) : base(item)
        {
        }

        public static List<string> Extensions => new List<string>() {
            ".md", ".markdown",
        };

        public string TextValue
        {
            get => textValue;
            set => SetProperty(ref textValue, value);
        }

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            try
            {
                if (Item.StorageItem is StorageFile file)
                {
                    var text = await FileIO.ReadTextAsync(file);
                    var displayText = text.Length < Constants.PreviewPane.TextCharacterLimit ? text : text.Remove(Constants.PreviewPane.TextCharacterLimit);
                    TextValue = displayText;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return new List<FileProperty>();
        }
    }
}