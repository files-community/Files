using Files.App.ViewModels.SettingsViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.SettingsPages
{
	public sealed partial class Multitasking : Page
	{
		public MultitaskingViewModel ViewModel { get; } = new();

		public Multitasking()
            => InitializeComponent();
    }
}