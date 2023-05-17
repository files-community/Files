// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Text.Json;

namespace Files.App.UserControls.MultitaskingControl
{
	public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
	{
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		private string? _Description = null;
		public string? Description
		{
			get => _Description;
			set => SetProperty(ref _Description, value);
		}

		private string? _ToolTipText;
		public string? ToolTipText
		{
			get => _ToolTipText;
			set => SetProperty(ref _ToolTipText, value);
		}

		private IconSource? _IconSource;
		public IconSource? IconSource
		{
			get => _IconSource;
			set => SetProperty(ref _IconSource, value);
		}

		private bool _AllowStorageItemDrop;
		public bool AllowStorageItemDrop
		{
			get => _AllowStorageItemDrop;
			set => SetProperty(ref _AllowStorageItemDrop, value);
		}

		private TabItemArguments? _TabItemArguments;
		public TabItemArguments? TabItemArguments
			=> Control?.NavigationArguments ?? _TabItemArguments;

		public TabItemControl Control { get; private set; }

		public TabItem()
		{
			Control = new();
		}

		public void Unload()
		{
			Control.ContentChanged -= mainPageViewModel.Control_ContentChanged;
			_TabItemArguments = Control?.NavigationArguments;
			Dispose();
		}

		public void Dispose()
		{
			Control?.Dispose();
			Control = null;
		}
	}

	public class TabItemArguments
	{
		private static readonly KnownTypesConverter TypesConverter = new();

		public Type InitialPageType { get; set; }

		public object NavigationArg { get; set; }

		public string Serialize()
		{
			return JsonSerializer.Serialize(this, TypesConverter.Options);
		}

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
