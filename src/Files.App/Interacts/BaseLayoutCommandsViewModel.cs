using CommunityToolkit.Mvvm.Input;
using Files.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Windows.Input;

namespace Files.App.Interacts
{
	public class BaseLayoutCommandsViewModel : IDisposable
	{
		#region Constructor

		public BaseLayoutCommandsViewModel(IBaseLayoutCommandImplementationModel commandsModel)
		{
			CommandsModel = commandsModel;

			InitializeCommands();
		}

		#endregion Constructor

		public IBaseLayoutCommandImplementationModel CommandsModel { get; }

		#region Command Initialization

		private void InitializeCommands()
		{
			ShowPropertiesCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.ShowProperties);
			OpenDirectoryInNewTabCommand = new AsyncRelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInNewTab);
			OpenDirectoryInNewPaneCommand = new RelayCommand<RoutedEventArgs>(CommandsModel.OpenDirectoryInNewPane);
			OpenInNewWindowItemCommand = new AsyncRelayCommand<RoutedEventArgs>(CommandsModel.OpenInNewWindowItem);
			CreateNewFileCommand = new RelayCommand<ShellNewEntry>(CommandsModel.CreateNewFile);
			ItemPointerPressedCommand = new AsyncRelayCommand<PointerRoutedEventArgs>(CommandsModel.ItemPointerPressed);
			PointerWheelChangedCommand = new RelayCommand<PointerRoutedEventArgs>(CommandsModel.PointerWheelChanged);
			DragOverCommand = new AsyncRelayCommand<DragEventArgs>(CommandsModel.DragOver);
			DropCommand = new AsyncRelayCommand<DragEventArgs>(CommandsModel.Drop);
			CreateFolderWithSelection = new AsyncRelayCommand<RoutedEventArgs>(CommandsModel.CreateFolderWithSelection);
		}

		#endregion Command Initialization

		#region Commands

		public ICommand ShowPropertiesCommand { get; private set; }

		public ICommand OpenDirectoryInNewTabCommand { get; private set; }

		public ICommand OpenDirectoryInNewPaneCommand { get; private set; }

		public ICommand OpenInNewWindowItemCommand { get; private set; }

		public ICommand CreateNewFileCommand { get; private set; }

		public ICommand ItemPointerPressedCommand { get; private set; }

		public ICommand PointerWheelChangedCommand { get; private set; }

		public ICommand DragOverCommand { get; private set; }

		public ICommand DropCommand { get; private set; }

		public ICommand CreateFolderWithSelection { get; private set; }

		#endregion Commands

		#region IDisposable

		public void Dispose()
		{
			CommandsModel?.Dispose();
		}

		#endregion IDisposable
	}
}
