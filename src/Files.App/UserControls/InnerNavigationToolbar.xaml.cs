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
using System.Windows.Input;

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

		public bool IsCompactOverlay
		{
			get { return (bool)GetValue(IsCompactOverlayProperty); }
			set { SetValue(IsCompactOverlayProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsCompactOverlay.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsCompactOverlayProperty =
			DependencyProperty.Register("IsCompactOverlay", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

		public ICommand SetCompactOverlayCommand
		{
			get { return (ICommand)GetValue(SetCompactOverlayCommandProperty); }
			set { SetValue(SetCompactOverlayCommandProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ToggleCompactOverlayCommand.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SetCompactOverlayCommandProperty =
			DependencyProperty.Register("ToggleCompactOverlayCommand", typeof(ICommand), typeof(AddressToolbar), new PropertyMetadata(null));

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
							Icon = new FontIcon()
							{
								Glyph = "\xE7C3"
							},
							Tag = "CreateNewFile"
						};
					}
					menuLayoutItem.Command = ViewModel.CreateNewFileCommand;
					menuLayoutItem.CommandParameter = newEntry;
					NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
				}
			}
		}

		private void NavToolbarDetailsHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView(true);
		private void NavToolbarTilesHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles(true);
		private void NavToolbarSmallIconsHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall(true);
		private void NavToolbarMediumIconsHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium(true);
		private void NavToolbarLargeIconsHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge(true);
		private void NavToolbarColumnsHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeColumnView(true);
		private void NavToolbarAdaptiveHeader_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeAdaptive();
	}
}