using Files.App.ViewModels;
using Files.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Files.App.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.App.UserControls.MultitaskingControl
{
    public class TabItem : ObservableObject, ITabItemControl
    {
        private string header;
        private string description;
        private string toolTipText;
        private IconSource iconSource;
        private bool allowStorageItemDrop;
        

        public string Header
        {
            get => header;
            set => SetProperty(ref header, value);
        }

        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        public string ToolTipText
        {
            get => toolTipText;
            set => SetProperty(ref toolTipText, value);
        }

        public IconSource IconSource
        {
            get => iconSource;
            set => SetProperty(ref iconSource, value);
        }

        public bool AllowStorageItemDrop
        {
            get => allowStorageItemDrop;
            set => SetProperty(ref allowStorageItemDrop, value);
        }
    }

    public class TabItemArguments
    {
        private static readonly KnownTypesConverter TypesConverter = new KnownTypesConverter();

        public PaneNavigationArguments NavigationArguments { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this, TypesConverter.Options);

        public static TabItemArguments Deserialize(string obj)
        {
            var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);

            return new TabItemArguments()
            {
                NavigationArguments = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs["NavigationArg"].GetRawText())
            };
        }
    }
}
