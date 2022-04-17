using Files.ViewModels;
using Files.Views;
using Files.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Files.Uwp.UserControls.MultitaskingControl
{
    public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
    {
        private string header;

        public string Header
        {
            get => header;
            set => SetProperty(ref header, value);
        }

        private string description;

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

        private bool isPinned = false;

        [SelectiveSerializationProperty]
        public bool IsPinned
        {
            get => isPinned;
            set => SetProperty(ref isPinned, value);
        }

        private TabItemArguments tabItemArguments;

        [SelectiveSerializationProperty(typeof(PaneNavigationArguments))]
        public TabItemArguments TabItemArguments
        {
            get => Control?.NavigationArguments ?? tabItemArguments;
            set
            {
                tabItemArguments = value;

                if (Control != null)
                {
                    Control.NavigationArguments = tabItemArguments;
                }
            }
        }

        public TabItem(bool initializetTabControl = true)
        {
            if (initializetTabControl)
            {
                Control = new TabItemControl();
            }
        }

        public void Unload()
        {
            if (Control != null)
            {
                Control.ContentChanged -= MainPageViewModel.Control_ContentChanged;
                tabItemArguments = Control?.NavigationArguments;
            }

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

        private static KnownTypeSerialization serializer = new KnownTypeSerialization(new Type[]
        {
            typeof(PaneNavigationArguments)
        });

        public string Serialize()
        {
            return serializer.Serialize(this);
        }

        public static TabItemArguments Deserialize(string obj)
        {
            return serializer.Deserialize<TabItemArguments>(obj);
        }
    }
}