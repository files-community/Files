// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
	public sealed partial class CompatibilityPage : BasePropertiesPage
	{
		private CompatibilityViewModel CompatibilityProperties { get; set; }

		public CompatibilityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			if (np.Parameter is ListedItem listedItem)
				CompatibilityProperties = new CompatibilityViewModel(listedItem);

			base.OnNavigatedTo(e);
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			CompatibilityProperties?.GetCompatibilityOptions();
		}

		public override Task<bool> SaveChangesAsync()
		{
			if (CompatibilityProperties is not null)
				return Task.FromResult(CompatibilityProperties.SetCompatibilityOptions());

			return Task.FromResult(false);
		}

		public override void Dispose()
		{
		}
	}
}
