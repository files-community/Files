using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Files.UserControls.MultitaskingControl
{
    public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
    {
        private string _Header;

        public string Header
        {
            get => _Header;
            set => SetProperty(ref _Header, value);
        }

        private string _Description = null;

        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private IconSource _IconSource;

        public IconSource IconSource
        {
            get => _IconSource;
            set => SetProperty(ref _IconSource, value);
        }

        public TabItemControl Control { get; private set; }

        private bool _AllowStorageItemDrop = false;

        public bool AllowStorageItemDrop
        {
            get => _AllowStorageItemDrop;
            set => SetProperty(ref _AllowStorageItemDrop, value);
        }

        private TabItemArguments _TabItemArguments;

        public TabItemArguments TabItemArguments
        {
            get => Control?.TabItemContent?.TabItemArguments ?? _TabItemArguments;
        }

        public TabItem()
        {
            Control = new TabItemControl();
        }

        public void Unload()
        {
            _TabItemArguments = Control?.TabItemContent?.TabItemArguments;
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