using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.DataModels;
using Files.App.ViewModels;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Linq;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class InnerNavigationToolbar : UserControl
	{
		public InnerNavigationToolbar()
		{
			InitializeComponent();
		}

		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

		public AppModel AppModel => App.AppModel;

		public PreviewPaneViewModel PreviewPaneViewModel => App.PreviewPaneViewModel;

		public ToolbarViewModel ViewModel
		{
			get => (ToolbarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ToolbarViewModel), typeof(InnerNavigationToolbar), new PropertyMetadata(null));

		public bool ShowPreviewPaneButton
		{
			get { return (bool)GetValue(ShowPreviewPaneButtonProperty); }
			set { SetValue(ShowPreviewPaneButtonProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowPreviewPaneButton.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowPreviewPaneButtonProperty =
			DependencyProperty.Register("ShowPreviewPaneButton", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

		public bool ShowMultiPaneControls
		{
			get => (bool)GetValue(ShowMultiPaneControlsProperty);
			set => SetValue(ShowMultiPaneControlsProperty, value);
		}

		// Using a DependencyProperty as the backing store for ShowMultiPaneControls.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowMultiPaneControlsProperty =
			DependencyProperty.Register(nameof(ShowMultiPaneControls), typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

		public bool IsMultiPaneActive
		{
			get { return (bool)GetValue(IsMultiPaneActiveProperty); }
			set { SetValue(IsMultiPaneActiveProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsMultiPaneActive.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsMultiPaneActiveProperty =
			DependencyProperty.Register("IsMultiPaneActive", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(false));

		private void NewEmptySpace_Opening(object sender, object e)
		{
			if (!ViewModel.InstanceViewModel.CanCreateFileInPage)
			{
				var shell = NewEmptySpace.Items.Where(x => (x.Tag as string) == "CreateNewFile").Reverse().ToList();
				shell.ForEach(x => NewEmptySpace.Items.Remove(x));
				return;
			}
			var cachedNewContextMenuEntries = addItemService.GetNewEntriesAsync().Result;
			if (cachedNewContextMenuEntries is null)
				return;
			if (!NewEmptySpace.Items.Any(x => (x.Tag as string) == "CreateNewFile"))
			{
				var separatorIndex = NewEmptySpace.Items.IndexOf(NewEmptySpace.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));

				ushort key = 0;
				string keyFormat = $"D{cachedNewContextMenuEntries.Count.ToString().Length}";

				foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
				{
					MenuFlyoutItem menuLayoutItem;
					if (!string.IsNullOrEmpty(newEntry.IconBase64))
					{
						byte[] bitmapData = Convert.FromBase64String(newEntry.IconBase64);
						using var ms = new MemoryStream(bitmapData);
						var image = new BitmapImage();
						_ = image.SetSourceAsync(ms.AsRandomAccessStream());
						menuLayoutItem = new MenuFlyoutItemWithImage()
						{
							Text = newEntry.Name,
							BitmapIcon = image,
							Tag = "CreateNewFile"
						};
					}
					else
					{
						menuLayoutItem = new MenuFlyoutItem()
						{
							Text = newEntry.Name,
							Icon = new FontIcon
							{
								Glyph = "\xE7C3"
							},
							Tag = "CreateNewFile"
						};
					}
					menuLayoutItem.AccessKey = (cachedNewContextMenuEntries.Count + 1 - (++key)).ToString(keyFormat);
					menuLayoutItem.Command = ViewModel.CreateNewFileCommand;
					menuLayoutItem.CommandParameter = newEntry;
					NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
				}
			}
		}

		private void SortGroup_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			if (sender is MenuFlyoutSubItem menu)
			{
				var items = menu.Items
					.TakeWhile(item => item is not MenuFlyoutSeparator)
					.Where(item => item.IsEnabled)
					.ToList();

				string format = $"D{items.Count.ToString().Length}";

				for (ushort index = 0; index < items.Count; ++index)
				{
					items[index].AccessKey = (index+1).ToString(format);
				}
			}

		}
	}
}