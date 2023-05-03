// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Helpers;
using Files.App.ViewModels;
using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Files.App.UserControls.MultitaskingControl
{
	public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
	{
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

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

		private string toolTipText;

		/// <summary>
		/// The text that should be displayed in the tooltip when hovering the tab item.
		/// </summary>
		public string ToolTipText
		{
			get => toolTipText;
			set => SetProperty(ref toolTipText, value);
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
			Control.ContentChanged -= mainPageViewModel.Control_ContentChanged;
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
		private static readonly KnownTypesConverter TypesConverter = new KnownTypesConverter();

		public Type InitialPageType { get; set; }
		public object NavigationArg { get; set; }

		public string Serialize() => JsonSerializer.Serialize(this, TypesConverter.Options);

		public static TabItemArguments Deserialize(string obj)
		{
			var tabArgs = new TabItemArguments();

			var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);
			tabArgs.InitialPageType = Type.GetType(tempArgs["InitialPageType"].GetString());

			try
			{
				tabArgs.NavigationArg = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs["NavigationArg"].GetRawText());
			}
			catch (JsonException)
			{
				tabArgs.NavigationArg = tempArgs["NavigationArg"].GetString();
			}

			return tabArgs;
		}
	}
}
