using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;

namespace Files.UserControls.MultitaskingControl
{
    public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
    {
        private string header;

        public string Header
        {
            get => header;
            set => SetProperty(ref header, value);
        }

        private string description = null;

        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private IconSource iconSource;

        public IconSource IconSource
        {
            get => iconSource;
            set => SetProperty(ref iconSource, value);
        }

        public TabItemControl Control { get; private set; }

        private bool allowStorageItemDrop;

        public bool AllowStorageItemDrop
        {
            get => allowStorageItemDrop;
            set => SetProperty(ref allowStorageItemDrop, value);
        }

        private TabItemArguments tabItemArguments;

        public TabItemArguments TabItemArguments
        {
            get => Control?.NavigationArguments ?? tabItemArguments;
        }

        public TabItem()
        {
            Control = new TabItemControl();
        }

        public void Unload()
        {
            tabItemArguments = Control?.NavigationArguments;
            Dispose();
        }

        #region IDisposable

        public void Dispose()
        {
            Control?.Dispose();
            Control = null;
        }

        #endregion IDisposable
    }

    public class TabItemArguments
    {
        public Type InitialPageType { get; set; }
        public object NavigationArg { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        public static TabItemArguments Deserialize(string obj) => JsonConvert.DeserializeObject<TabItemArguments>(obj, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
    }
}