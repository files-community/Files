﻿using Files.Filesystem;
using Files.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.FilePreviews
{
    public abstract class PreviewControlBase : UserControl
    {
        public ListedItem Item { get; internal set; }

        public StorageFile ItemFile { get; internal set; }

        public CancellationTokenSource LoadCancelledTokenSource { get; } = new CancellationTokenSource();

        public PreviewControlBase(ListedItem item) : base()
        {
            Item = item;
            Unloaded += PreviewControlBase_Unloaded;
            Load();
        }

        public virtual void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
        {
            LoadCancelledTokenSource.Cancel();
        }

        public abstract void LoadPreviewAndDetails();

        public async void LoadSystemFileProperties()
        {
            if (Item.IsShortcutItem)
            {
                return;
            }

            var list = await FileProperty.RetrieveAndInitializePropertiesAsync(ItemFile);

            list.Find(x => x.ID == "address").Value = await FileProperties.GetAddressFromCoordinatesAsync((double?)list.Find(x => x.Property == "System.GPS.LatitudeDecimal").Value,
                                                                                           (double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

            list.Where(i => i.Value != null).ToList().ForEach(x => Item.FileDetails.Add(x));
        }

        private async void Load()
        {
            // Files can be corrupt, in use, and stuff
            try
            {
                ItemFile ??= await StorageFile.GetFileFromPathAsync(Item.ItemPath);
                LoadPreviewAndDetails();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
