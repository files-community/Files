using Files.App.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class FolderEmptyIndicator : UserControl
	{
		public EmptyTextType EmptyTextType
		{
			get => (EmptyTextType)GetValue(EmptyTextTypeProperty);
			set => SetValue(EmptyTextTypeProperty, value);
		}

		public static readonly DependencyProperty EmptyTextTypeProperty =
			DependencyProperty.Register(
				"EmptyTextType",
				typeof(EmptyTextType),
				typeof(FolderEmptyIndicator),
				new PropertyMetadata(null));

		public FolderEmptyIndicator()
		{
			InitializeComponent();
		}

		private string GetTranslated(string resourceName)
			=> resourceName.GetLocalizedResource();
	}

	public enum EmptyTextType
	{
		None,

		FolderEmpty,

		NoSearchResultsFound,
	}
}
