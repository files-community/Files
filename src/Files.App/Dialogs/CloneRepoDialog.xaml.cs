// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Dialogs
{
	public sealed partial class CloneRepoDialog : ContentDialog, IDialog<CloneRepoDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public CloneRepoDialogViewModel ViewModel
		{
			get => (CloneRepoDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CloneRepoDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
