using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Control.ContentChanged -= MainPageViewModel.Control_ContentChanged;
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
        private static KnownTypesBinder TypesBinder = new KnownTypesBinder
        {
            KnownTypes = new List<Type> { typeof(PaneNavigationArguments) }
        };

        public Type InitialPageType { get; set; }
        public object NavigationArg { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = TypesBinder
        });

        public static TabItemArguments Deserialize(string obj) => JsonConvert.DeserializeObject<TabItemArguments>(obj, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = TypesBinder
        });
    }

    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public Type BindToType(string assemblyName, string typeName)
        {
            if (!KnownTypes.Any(x => x.Name == typeName))
            {
                throw new ArgumentException();
            }
            else
            {
                return KnownTypes.SingleOrDefault(t => t.Name == typeName);
            }
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
}