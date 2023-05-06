using CommunityToolkit.WinUI.UI;
using Files.App.ServicesImplementation.Settings;
using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class PDFPreview : UserControl
	{
		
		private readonly bool IsLeftToRight;
		private int lastIndex;
		private bool isFirstPage;
		private bool isLastPage;
		private void PreviousButton_Click(object sender, RoutedEventArgs e)
		{
			if (IsLeftToRight)
			{
				NavigatePage(-1);
			}
			else
			{
				NavigatePage(1);
			}

		}

		private void NextButton_Click(object sender, RoutedEventArgs e)
		{
			if (IsLeftToRight)
			{
				NavigatePage(1);
			}
			else
			{
				NavigatePage(-1);
			}
		}

		private void NavigatePage(int direction)
		{
			int nextPageIndex = PageList.SelectedIndex + direction;

			if (nextPageIndex >= 0 && nextPageIndex < PageList.Items.Count)
			{
				PageList.SelectedIndex = nextPageIndex;
			}
		}

		public PDFPreview(PDFPreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();
			// Determine whether the user is using LTR or RTL flowdirection
			IsLeftToRight = PageList.FlowDirection == FlowDirection.LeftToRight;
			
			PageList.SelectionChanged += PageList_SelectionChanged;

		}

		private void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Check whether the selected page is the first or last page
			lastIndex = PageList.Items.Count - 1;
			isFirstPage = PageList.SelectedIndex == 0;
			isLastPage = PageList.SelectedIndex == lastIndex;

			// Determine whether to hide the left and/or right arrow based on the page direction
			if (IsLeftToRight)
			{
				// Hide the left arrow if the selected page is the first page,
				// and hide the right arrow if the selected page is the last page
				// Update the visibility of the left and right arrows based on whether they should be hidden or not
				LeftArrow.Visibility = isFirstPage ? Visibility.Collapsed : Visibility.Visible;
				RightArrow.Visibility = isLastPage ? Visibility.Collapsed : Visibility.Visible;

			}
			else
			{
				// hide the right arrow if the selected page is the first page,
				// and hide the left arrow if the selected page is the last page
				// Update the visibility of the left and right arrows based on whether they should be hidden or not
				LeftArrow.Visibility = isLastPage ? Visibility.Collapsed : Visibility.Visible;
				RightArrow.Visibility = isFirstPage ? Visibility.Collapsed : Visibility.Visible;

			}

		}

		public PDFPreviewViewModel ViewModel { get; set; }
	}
}