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
using System.Linq;
using static Files.App.Constants;

namespace Files.App.Views.LayoutModes
{
	public sealed partial class ColumnViewBrowser : BaseLayout
	{
		protected override uint IconSize => Browser.ColumnViewBrowser.ColumnViewSizeSmall;
		protected override ItemsControl ItemsControl => ColumnHost;

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

		private void ColumnViewBase_ItemInvoked(object sender, EventArgs e)
		{
			var column = sender as ColumnParam;
			if (column.ListView.FindAscendant<ColumnViewBrowser>() != this)
			{
				return;
			}

			var nextBladeIndex = ColumnHost.ActiveBlades.IndexOf(column.ListView.FindAscendant<BladeItem>()) + 1;

			if (ColumnHost.ActiveBlades.ElementAtOrDefault(nextBladeIndex) is not BladeItem nextBlade ||
				((nextBlade.Content as Frame)?.Content as IShellPage)?.FilesystemViewModel?.WorkingDirectory != column.NavPathParam)
			{
				DismissOtherBlades(column.ListView);

				var frame = new Frame();
				frame.Navigated += Frame_Navigated;
				var newblade = new BladeItem();
				newblade.Content = frame;
				ColumnHost.Items.Add(newblade);

				frame.Navigate(typeof(ColumnShellPage), new ColumnParam
				{
					Column = ColumnHost.ActiveBlades.IndexOf(newblade),
					NavPathParam = column.NavPathParam
				});
			}
		}

		private void ContentChanged(IShellPage p)
		{
			(ParentShellPageInstance as ModernShellPage)?.RaiseContentChanged(p, p.TabItemArguments);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			var navigationArguments = (NavigationArguments)eventArgs.Parameter;
			MainPageFrame.Navigated += Frame_Navigated;
			MainPageFrame.Navigate(typeof(ColumnShellPage), new ColumnParam
			{
				Column = 0,
				IsSearchResultPage = navigationArguments.IsSearchResultPage,
				SearchQuery = navigationArguments.SearchQuery,
				SearchUnindexedItems = navigationArguments.SearchUnindexedItems,
				SearchPathParam = navigationArguments.SearchPathParam,
				NavPathParam = navigationArguments.NavPathParam
			});
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
			ColumnHost.Items.OfType<BladeItem>().Select(x => ((x.Content as Frame)?.Content as ColumnShellPage).SlimContentPage as ColumnViewBase).Where(x => x is not null).ForEach(x => x.ItemInvoked -= ColumnViewBase_ItemInvoked);
			ColumnHost.Items.OfType<BladeItem>().ForEach(x => ((x.Content as Frame)?.Content as ColumnShellPage).ContentChanged -= ColumnViewBrowser_ContentChanged);
			ColumnHost.Items.OfType<BladeItem>().ForEach(x => ((x.Content as Frame)?.Content as UIElement).GotFocus -= ColumnViewBrowser_GotFocus);
			ColumnHost.Items.OfType<BladeItem>().Select(x => (x.Content as Frame)?.Content).OfType<IDisposable>().ForEach(x => x.Dispose());
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
						if ((ColumnHost.ActiveBlades[index + 1].Content as Frame)?.Content is IDisposable disposableContent)
						{
							disposableContent.Dispose();
						}
						if (((ColumnHost.ActiveBlades[index + 1].Content as Frame).Content as ColumnShellPage)?.SlimContentPage is ColumnViewBase columnLayout)
						{
							columnLayout.ItemInvoked -= ColumnViewBase_ItemInvoked;
						}
						((ColumnHost.ActiveBlades[index + 1].Content as Frame).Content as UIElement).GotFocus -= ColumnViewBrowser_GotFocus;
						((ColumnHost.ActiveBlades[index + 1].Content as Frame).Content as ColumnShellPage).ContentChanged -= ColumnViewBrowser_ContentChanged;
						ColumnHost.Items.RemoveAt(index + 1);
						ColumnHost.ActiveBlades.RemoveAt(index + 1);
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
			if (!(sender as IShellPage).IsCurrentInstance)
			{
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
				(sender as IShellPage).IsCurrentInstance = true;
				ContentChanged(sender as IShellPage);
			}
		}

		private void ColumnViewBrowser_ContentChanged(object sender, UserControls.MultitaskingControl.TabItemArguments e)
		{
			var c = sender as IShellPage;
			(c.SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked;
			(c.SlimContentPage as ColumnViewBase).ItemInvoked += ColumnViewBase_ItemInvoked;
			ContentChanged(c);
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
			{
				DismissOtherBlades(ColumnHost.ActiveBlades[ColumnHost.ActiveBlades.Count - 2]);
			}
			else
			{
				(ParentShellPageInstance as ModernShellPage)?.Up_Click();
			}
		}

		public void MoveFocusToPreviousBlade(int currentBladeIndex)
		{
			if (currentBladeIndex <= 0)
				return;

			DismissOtherBlades(currentBladeIndex);

			var activeBlade = ColumnHost.ActiveBlades[currentBladeIndex - 1];
			activeBlade.Focus(FocusState.Programmatic);

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

			var activeBladeColumnViewBase = RetrieveBladeColumnViewBase(activeBlade);
			if (activeBladeColumnViewBase is not null)
				activeBladeColumnViewBase.FileList.SelectedIndex = 0;
		}

		private ColumnViewBase? RetrieveBladeColumnViewBase(BladeItem blade)
		{
			if (blade.Content is not Frame activeBladeFrame)
				return null;

			if (activeBladeFrame.Content is not ColumnShellPage activeBladePage)
				return null;

			return activeBladePage.SlimContentPage as ColumnViewBase;
		}

		public void SetSelectedPathOrNavigate(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			var destPath = navArgs is not null ? (navArgs.IsSearchResultPage ? navArgs.SearchPathParam : navArgs.NavPathParam) : navigationPath;
			var columnPath = ((ColumnHost.ActiveBlades.Last().Content as Frame)?.Content as ColumnShellPage)?.FilesystemViewModel.WorkingDirectory;
			var columnFirstPath = ((ColumnHost.ActiveBlades.First().Content as Frame)?.Content as ColumnShellPage)?.FilesystemViewModel.WorkingDirectory;

			if (string.IsNullOrEmpty(destPath) || string.IsNullOrEmpty(destPath) || string.IsNullOrEmpty(destPath))
			{
				ParentShellPageInstance.NavigateToPath(navigationPath, sourcePageType, navArgs);
				return;
			}

			var destComponents = StorageFileExtensions.GetDirectoryPathComponents(destPath);
			var columnComponents = StorageFileExtensions.GetDirectoryPathComponents(columnPath);
			var columnFirstComponents = StorageFileExtensions.GetDirectoryPathComponents(columnFirstPath);

			var lastCommonItemIndex = columnComponents
				.Select((value, index) => new { value, index })
				.LastOrDefault(x => x.index < destComponents.Count && x.value.Path == destComponents[x.index].Path)?.index ?? -1;

			var relativeIndex = lastCommonItemIndex - (columnFirstComponents.Count - 1);

			if (relativeIndex < 0 || destComponents.Count - (lastCommonItemIndex + 1) > 1) // Going above parent or too deep down
			{
				ParentShellPageInstance.NavigateToPath(navigationPath, sourcePageType, navArgs);
			}
			else
			{
				DismissOtherBlades(relativeIndex);

				for (int ii = lastCommonItemIndex + 1; ii < destComponents.Count; ii++)
				{
					var frame = new Frame();
					frame.Navigated += Frame_Navigated;
					var newblade = new BladeItem();
					newblade.Content = frame;
					ColumnHost.Items.Add(newblade);

					if (navArgs is not null)
					{
						frame.Navigate(typeof(ColumnShellPage), new ColumnParam
						{
							Column = ColumnHost.ActiveBlades.IndexOf(newblade),
							IsSearchResultPage = navArgs.IsSearchResultPage,
							SearchQuery = navArgs.SearchQuery,
							NavPathParam = destComponents[ii].Path,
							SearchUnindexedItems = navArgs.SearchUnindexedItems,
							SearchPathParam = navArgs.SearchPathParam
						});
					}
					else
					{
						frame.Navigate(typeof(ColumnShellPage), new ColumnParam
						{
							Column = ColumnHost.ActiveBlades.IndexOf(newblade),
							NavPathParam = destComponents[ii].Path
						});
					}
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
				DismissOtherBlades(ColumnHost.ActiveBlades[0]);
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
	}
}