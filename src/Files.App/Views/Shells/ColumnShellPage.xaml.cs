// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace Files.App.Views.Shells
{
	public sealed partial class ColumnShellPage : BaseShellPage
	{
		public override bool IsCurrentPane
			=> this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.IsCurrentPane ?? false;

		public override bool CanNavigateBackward
			=> false;

		public override bool CanNavigateForward
			=> false;

		protected override Frame ItemDisplay
			=> ItemDisplayFrame;

		private ColumnParam _ColumnParams;
		public ColumnParam ColumnParams
		{
			get => _ColumnParams;
			set
			{
				if (value != _ColumnParams)
				{
					_ColumnParams = value;

					if (IsLoaded)
						OnNavigationParamsChanged();
				}
			}
		}

		public ColumnShellPage() : base(new CurrentInstanceViewModel(FolderLayoutModes.ColumnView))
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			ColumnParams = eventArgs.Parameter as ColumnParam;
			if (ColumnParams?.IsLayoutSwitch ?? false)
				FilesystemViewModel_DirectoryInfoUpdated(this, EventArgs.Empty);
		}

		protected override void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			this.FindAscendant<ColumnsLayoutPage>()?.SetSelectedPathOrNavigate(e);
		}

		protected override void OnNavigationParamsChanged()
		{
			if (ColumnParams.NavPathParam is not null)
				// This method call is required to load the sorting preferences.
				InstanceViewModel.FolderSettings.GetLayoutType(ColumnParams.NavPathParam);

			ItemDisplayFrame.Navigate(
				typeof(ColumnLayoutPage),
				new NavigationArguments()
				{
					IsSearchResultPage = ColumnParams.IsSearchResultPage,
					SearchQuery = ColumnParams.SearchQuery,
					NavPathParam = ColumnParams.NavPathParam,
					SearchPathParam = ColumnParams.SearchPathParam,
					AssociatedTabInstance = this,
					SelectItems = ColumnParams.SelectItems
				});
		}

		protected override void Page_Loaded(object sender, RoutedEventArgs e)
		{
			ShellViewModel = new ShellViewModel(InstanceViewModel?.FolderSettings);
			ShellViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
			ShellViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
			ShellViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
			ShellViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			ShellViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
			ShellViewModel.GitDirectoryUpdated += FilesystemViewModel_GitDirectoryUpdated;

			PaneHolder = this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.PaneHolder;

			base.Page_Loaded(sender, e);

			NotifyPropertyChanged(nameof(ShellViewModel));
		}

		protected override async void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
			string value = e.Path;
			if (!string.IsNullOrWhiteSpace(value))
				await UpdatePathUIToWorkingDirectoryAsync(value);
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ContentPage = await GetContentOrNullAsync();
			
			if (ItemDisplayFrame.CurrentSourcePageType == typeof(ColumnLayoutPage))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage.ResetItemOpacity();
			}

			var parameters = e.Parameter as NavigationArguments;
			TabBarItemParameter = new TabBarItemParameter()
			{
				InitialPageType = typeof(ColumnShellPage),
				NavigationParameter = parameters.IsSearchResultPage ? parameters.SearchPathParam : parameters.NavPathParam
			};
		}

		public override void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (ItemDisplayFrame.CanGoBack)
				base.Back_Click();
			else
				this.FindAscendant<ColumnsLayoutPage>().NavigateBack();
		}

		public override void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (ItemDisplayFrame.CanGoForward)
				base.Forward_Click();
			else
				this.FindAscendant<ColumnsLayoutPage>().NavigateForward();
		}

		public override void Up_Click()
		{
			if (!ToolbarViewModel.CanNavigateToParent)
				return;

			this.FindAscendant<ColumnsLayoutPage>()?.NavigateUp();
		}

		public override void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			this.FindAscendant<ColumnsLayoutPage>()?.SetSelectedPathOrNavigate(navigationPath, sourcePageType, navArgs);
		}

		public override void NavigateHome()
		{
			this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.NavigateHome();
		}
		
		public override void NavigateToReleaseNotes()
		{
			this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.NavigateToReleaseNotes();
		}

		public override Task WhenIsCurrent()
			=> Task.WhenAll(_IsCurrentInstanceTCS.Task, this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.WhenIsCurrent() ?? Task.CompletedTask);
	}
}
