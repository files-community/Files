using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Interacts;
using Files.App.UserControls;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using static Files.App.Constants;
using static Files.App.Helpers.PathNormalization;

namespace Files.App.Views.LayoutModes
{
	public sealed partial class ColumnViewBrowser : BaseLayout
	{
		protected override uint IconSize => Browser.ColumnViewBrowser.ColumnViewSizeSmall;
		protected override ItemsControl ItemsControl => ColumnHost;

		public string? OwnerPath { get; private set; }
		public int FocusIndex { get; private set; }

		public ColumnViewBrowser() : base()
		{
			InitializeComponent();
		}

		public void HandleSelectionChange(ColumnViewBase initiator)
		{
			foreach (var blade in ColumnHost.ActiveBlades)
			{
				var columnView = blade.FindDescendant<ColumnViewBase>();
				if (columnView != null && columnView != initiator)
					columnView.ClearSelectionIndicator();
			}
		}

		protected override void HookEvents()
		{
		}

		protected override void UnhookEvents()
		{
		}

		protected override bool CanGetItemFromElement(object element)
			=> false;

		private void ColumnViewBase_ItemInvoked(object? sender, EventArgs e)
		{
			var column = sender as ColumnParam;
			if (column?.ListView.FindAscendant<ColumnViewBrowser>() != this)
				return;

			var nextBladeIndex = ColumnHost.ActiveBlades.IndexOf(column.ListView.FindAscendant<BladeItem>()) + 1;

			if (ColumnHost.ActiveBlades.ElementAtOrDefault(nextBladeIndex) is not BladeItem nextBlade ||
				((nextBlade.Content as Frame)?.Content as IShellPage)?.FilesystemViewModel?.WorkingDirectory != column.NavPathParam)
			{
				DismissOtherBlades(column.ListView);

				var (frame, newblade) = CreateAndAddNewBlade();

				frame.Navigate(typeof(ColumnShellPage), new ColumnParam
				{
					Column = ColumnHost.ActiveBlades.IndexOf(newblade),
					NavPathParam = column.NavPathParam
				});
				navigationArguments.NavPathParam = column.NavPathParam;
				ParentShellPageInstance.TabItemArguments.NavigationArg = column.NavPathParam;
			}
		}

		private void ContentChanged(IShellPage p)
		{
			(ParentShellPageInstance as ModernShellPage)?.RaiseContentChanged(p, p.TabItemArguments);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			var path = navigationArguments.NavPathParam;
			var pathStack = new Stack<string>();

			if (path is not null)
			{
				var rootPathList = App.QuickAccessManager.Model.FavoriteItems.Select(x => NormalizePath(x)).ToList();
				rootPathList.Add(NormalizePath(GetPathRoot(path)));

				while (!rootPathList.Contains(NormalizePath(path)))
				{
					pathStack.Push(path);
					path = GetParentDir(path);
				}
			}

			OwnerPath = navigationArguments.NavPathParam;
			FocusIndex = pathStack.Count;

			MainPageFrame.Navigated += Frame_Navigated;
			MainPageFrame.Navigate(typeof(ColumnShellPage), new ColumnParam
			{
				Column = 0,
				IsSearchResultPage = navigationArguments.IsSearchResultPage,
				SearchQuery = navigationArguments.SearchQuery,
				SearchUnindexedItems = navigationArguments.SearchUnindexedItems,
				SearchPathParam = navigationArguments.SearchPathParam,
				NavPathParam = path
			});
			var index = 0;
			while (pathStack.TryPop(out path))
			{
				var (frame, _) = CreateAndAddNewBlade();

				frame.Navigate(typeof(ColumnShellPage), new ColumnParam
				{
					Column = ++index,
					NavPathParam = path
				});
			}
		}

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			Dispose();
		}

		#region IDisposable

		public override void Dispose()
		{
			base.Dispose();
			var columnHostItems = ColumnHost.Items.OfType<BladeItem>().Select(blade => blade.Content as Frame);
			foreach (var frame in columnHostItems)
			{
				if (frame?.Content is ColumnShellPage shPage)
				{
					shPage.ContentChanged -= ColumnViewBrowser_ContentChanged;
					if (shPage.SlimContentPage is ColumnViewBase viewBase)
					{
						viewBase.ItemInvoked -= ColumnViewBase_ItemInvoked;
						viewBase.ItemTapped -= ColumnViewBase_ItemTapped;
						viewBase.KeyUp -= ColumnViewBase_KeyUp;
					}
				}
				if (frame?.Content is UIElement element)
					element.GotFocus -= ColumnViewBrowser_GotFocus;
				if (frame?.Content is IDisposable disposable)
					disposable.Dispose();
			}
			UnhookEvents();
			CommandsViewModel?.Dispose();
		}

		#endregion IDisposable

		private void DismissOtherBlades(ListView listView)
		{
			DismissOtherBlades(listView.FindAscendant<BladeItem>());
		}

		private void DismissOtherBlades(BladeItem blade)
		{
			DismissOtherBlades(ColumnHost.ActiveBlades.IndexOf(blade));
		}

		private void DismissOtherBlades(int index)
		{
			if (index >= 0)
			{
				SafetyExtensions.IgnoreExceptions(() =>
				{
					while (ColumnHost.ActiveBlades.Count > index + 1)
					{
						var frame = ColumnHost.ActiveBlades[index + 1].Content as Frame;
						if (frame?.Content is IDisposable disposableContent)
							disposableContent.Dispose();
						if ((frame?.Content as ColumnShellPage)?.SlimContentPage is ColumnViewBase columnLayout)
						{
							columnLayout.ItemInvoked -= ColumnViewBase_ItemInvoked;
							columnLayout.ItemTapped -= ColumnViewBase_ItemTapped;
							columnLayout.KeyUp -= ColumnViewBase_KeyUp;
						}
						(frame?.Content as UIElement).GotFocus -= ColumnViewBrowser_GotFocus;
						(frame?.Content as ColumnShellPage).ContentChanged -= ColumnViewBrowser_ContentChanged;
						ColumnHost.Items.RemoveAt(index + 1);
						ColumnHost.ActiveBlades.RemoveAt(index + 1);
					}
					if ((ColumnHost.ActiveBlades[index].Content as Frame)?.Content is ColumnShellPage s)
					{
						navigationArguments.NavPathParam = s.FilesystemViewModel.WorkingDirectory;
						ParentShellPageInstance.TabItemArguments.NavigationArg = s.FilesystemViewModel.WorkingDirectory;
					}
				});
			}
			ContentChanged(ActiveColumnShellPage);
		}

		private void Frame_Navigated(object sender, NavigationEventArgs e)
		{
			var f = sender as Frame;
			f.Navigated -= Frame_Navigated;
			(f.Content as IShellPage).ContentChanged += ColumnViewBrowser_ContentChanged;
			(f.Content as UIElement).GotFocus += ColumnViewBrowser_GotFocus;
		}

		private void ColumnViewBrowser_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is not IShellPage shPage || shPage.IsCurrentInstance)
				return;
			var currentBlade = ColumnHost.ActiveBlades.Single(x => (x.Content as Frame)?.Content == sender);
			currentBlade.StartBringIntoView();
			if (ColumnHost.ActiveBlades is not null)
			{
				ColumnHost.ActiveBlades.ForEach(x =>
				{
					var shellPage = (x.Content as Frame)?.Content as ColumnShellPage;
					shellPage.IsCurrentInstance = false;
				});
			}
			shPage.IsCurrentInstance = true;
			ContentChanged(shPage);
		}

		private void ColumnViewBrowser_ContentChanged(object sender, UserControls.MultitaskingControl.TabItemArguments e)
		{
			var c = sender as IShellPage;
			var columnView = c?.SlimContentPage as ColumnViewBase;
			if (columnView is not null)
			{
				columnView.ItemInvoked -= ColumnViewBase_ItemInvoked;
				columnView.ItemInvoked += ColumnViewBase_ItemInvoked;
				columnView.ItemTapped -= ColumnViewBase_ItemTapped;
				columnView.ItemTapped += ColumnViewBase_ItemTapped;
				columnView.KeyUp -= ColumnViewBase_KeyUp;
				columnView.KeyUp += ColumnViewBase_KeyUp;
			}
			ContentChanged(c);
		}

		private void ColumnViewBase_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			CloseUnnecessaryColumns((ActiveColumnShellPage as ColumnShellPage)?.ColumnParams);
		}

		public void NavigateBack()
		{
			(ParentShellPageInstance as ModernShellPage)?.Back_Click();
		}

		public void NavigateForward()
		{
			(ParentShellPageInstance as ModernShellPage)?.Forward_Click();
		}

		public void NavigateUp()
		{
			if (ColumnHost.ActiveBlades?.Count > 1)
				DismissOtherBlades(ColumnHost.ActiveBlades[ColumnHost.ActiveBlades.Count - 2]);
			else
				(ParentShellPageInstance as ModernShellPage)?.Up_Click();
		}

		public void MoveFocusToPreviousBlade(int currentBladeIndex)
		{
			if (currentBladeIndex <= 0)
				return;

			DismissOtherBlades(currentBladeIndex);

			var activeBlade = ColumnHost.ActiveBlades[currentBladeIndex - 1];
			activeBlade.Focus(FocusState.Programmatic);
			FocusIndex = currentBladeIndex - 1;

			var activeBladeColumnViewBase = RetrieveBladeColumnViewBase(activeBlade);
			if (activeBladeColumnViewBase is null)
				return;

			//This allows to deselect and reselect the parent folder, hence forcing the refocus.
			var selectedItem = activeBladeColumnViewBase.FileList.SelectedItem;
			activeBladeColumnViewBase.FileList.SelectedItem = null;
			activeBladeColumnViewBase.FileList.SelectedItem = selectedItem;
		}

		public void MoveFocusToNextBlade(int currentBladeIndex)
		{
			if (currentBladeIndex >= ColumnHost.ActiveBlades.Count)
				return;

			var activeBlade = ColumnHost.ActiveBlades[currentBladeIndex];
			activeBlade.Focus(FocusState.Programmatic);
			FocusIndex = currentBladeIndex;

			var activeBladeColumnViewBase = RetrieveBladeColumnViewBase(activeBlade);
			if (activeBladeColumnViewBase is not null)
				activeBladeColumnViewBase.FileList.SelectedIndex = 0;
		}

		private ColumnViewBase? RetrieveBladeColumnViewBase(BladeItem blade)
		{
			if (blade.Content is not Frame activeBladeFrame || activeBladeFrame.Content is not ColumnShellPage activeBladePage)
				return null;
			return activeBladePage.SlimContentPage as ColumnViewBase;
		}

		public void SetSelectedPathOrNavigate(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			var destPath = navArgs is not null ? (navArgs.IsSearchResultPage ? navArgs.SearchPathParam : navArgs.NavPathParam) : navigationPath;
			var columnPath = ((ColumnHost.ActiveBlades.Last().Content as Frame)?.Content as ColumnShellPage)?.FilesystemViewModel.WorkingDirectory;
			var columnFirstPath = ((ColumnHost.ActiveBlades.First().Content as Frame)?.Content as ColumnShellPage)?.FilesystemViewModel.WorkingDirectory;

			if (string.IsNullOrEmpty(destPath) || string.IsNullOrEmpty(columnPath) || string.IsNullOrEmpty(columnFirstPath))
			{
				ParentShellPageInstance?.NavigateToPath(navigationPath, sourcePageType, navArgs);
				return;
			}

			var destComponents = StorageFileExtensions.GetDirectoryPathComponents(destPath);

			var (lastCommonItemIndex, relativeIndex) = GetLastCommonAndRelativeIndex(destComponents, columnPath, columnFirstPath);

			if (relativeIndex < 0 || destComponents.Count - (lastCommonItemIndex + 1) > 1) // Going above parent or too deep down
			{
				ParentShellPageInstance?.NavigateToPath(navigationPath, sourcePageType, navArgs);
			}
			else
			{
				DismissOtherBlades(relativeIndex);

				for (int ii = lastCommonItemIndex + 1; ii < destComponents.Count; ii++)
				{
					var (frame, newblade) = CreateAndAddNewBlade();

					var columnParam = new ColumnParam
					{
						Column = ColumnHost.ActiveBlades.IndexOf(newblade),
						NavPathParam = destComponents[ii].Path
					};
					if (navArgs is not null)
					{
						columnParam.IsSearchResultPage = navArgs.IsSearchResultPage;
						columnParam.SearchQuery = navArgs.SearchQuery;
						columnParam.SearchUnindexedItems = navArgs.SearchUnindexedItems;
						columnParam.SearchPathParam = navArgs.SearchPathParam;
					}
					frame.Navigate(typeof(ColumnShellPage), columnParam);
				}
			}
		}

		public void SetSelectedPathOrNavigate(PathNavigationEventArgs e)
		{
			if (ColumnHost.ActiveBlades?.Count > 1)
			{
				foreach (var item in ColumnHost.ActiveBlades)
				{
					if ((item.Content as Frame)?.Content is ColumnShellPage s &&
						PathNormalization.NormalizePath(s.FilesystemViewModel.WorkingDirectory) ==
						PathNormalization.NormalizePath(e.ItemPath))
					{
						DismissOtherBlades(item);
						return;
					}
				}
			}
			if (PathNormalization.NormalizePath(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory) !=
				PathNormalization.NormalizePath(e.ItemPath))
			{
				ParentShellPageInstance.NavigateToPath(e.ItemPath);
			}
			else
			{
				DismissOtherBlades(0);
			}
		}

		public IShellPage ActiveColumnShellPage
		{
			get
			{
				if (ColumnHost.ActiveBlades?.Count > 0)
				{
					var shellPages = ColumnHost.ActiveBlades.Select(x => (x.Content as Frame).Content as IShellPage);
					var activeInstance = shellPages.SingleOrDefault(x => x.IsCurrentInstance);
					return activeInstance ?? shellPages.Last();
				}

				return ParentShellPageInstance;
			}
		}

		private void ColumnViewBase_ItemTapped(object? sender, EventArgs e)
		{
			var column = sender as ColumnParam;
			if (column?.ListView.FindAscendant<ColumnViewBrowser>() != this || string.IsNullOrEmpty(column.NavPathParam))
				return;
			
			CloseUnnecessaryColumns(column);
		}

		private void CloseUnnecessaryColumns(ColumnParam column)
		{
			var columnPath = ((ColumnHost.ActiveBlades.Last().Content as Frame)?.Content as ColumnShellPage)?.FilesystemViewModel.WorkingDirectory;
			var columnFirstPath = ((ColumnHost.ActiveBlades.First().Content as Frame)?.Content as ColumnShellPage)?.FilesystemViewModel.WorkingDirectory;
			if (string.IsNullOrEmpty(columnPath) || string.IsNullOrEmpty(columnFirstPath))
				return;

			var destComponents = StorageFileExtensions.GetDirectoryPathComponents(column.NavPathParam);
			var (_, relativeIndex) = GetLastCommonAndRelativeIndex(destComponents, columnPath, columnFirstPath);
			if (relativeIndex >= 0)
				DismissOtherBlades(relativeIndex);
		}

		private (int, int) GetLastCommonAndRelativeIndex(List<PathBoxItem> destComponents, string columnPath, string columnFirstPath)
		{
			var columnComponents = StorageFileExtensions.GetDirectoryPathComponents(columnPath);
			var columnFirstComponents = StorageFileExtensions.GetDirectoryPathComponents(columnFirstPath);

			var lastCommonItemIndex = columnComponents
				.Select((value, index) => new { value, index })
				.LastOrDefault(x => x.index < destComponents.Count && x.value.Path == destComponents[x.index].Path)?.index ?? -1;

			var relativeIndex = lastCommonItemIndex - (columnFirstComponents.Count - 1);

			return (lastCommonItemIndex, relativeIndex);
		}

		private (Frame, BladeItem) CreateAndAddNewBlade()
		{
			var frame = new Frame();
			frame.Navigated += Frame_Navigated;
			var newblade = new BladeItem()
			{
				Content = frame
			};
			ColumnHost.Items.Add(newblade);
			return (frame, newblade);
		}
	}
}