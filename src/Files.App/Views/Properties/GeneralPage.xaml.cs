using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class GeneralPage : BasePropertiesPage
	{
		public GeneralViewModel GeneralViewModel { get; }

		private readonly Regex letterRegex = new(@"\s*\(\w:\)$");

		public GeneralPage()
		{
			InitializeComponent();

			GeneralViewModel = new();
		}

		private void ItemFileName_GettingFocus(UIElement _, GettingFocusEventArgs e)
		{
			// Remove drive letter in getting focus
			ItemFileName.Text = letterRegex.Replace(ItemFileName.Text, string.Empty);
		}

		private void ItemFileName_LosingFocus(UIElement _, LosingFocusEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(ItemFileName.Text))
			{
				ItemFileName.Text = ViewModel.ItemName;
				return;
			}

			// Add drive letter in losing focus
			var match = letterRegex.Match(ViewModel.OriginalItemName);
			if (match.Success)
				ItemFileName.Text += match.Value;
		}

		public override async Task<bool> SaveChangesAsync()
			=> await GeneralViewModel.SaveChanges();

		public override void Dispose()
		{
		}
	}
}
