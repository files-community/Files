// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
    public sealed partial class SignaturesPage : BasePropertiesPage
    {
        private SignaturesViewModel SignaturesViewModel { get; set; }

        public SignaturesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var np = (PropertiesPageNavigationParameter)e.Parameter;
            if (np.Parameter is ListedItem listedItem)
                SignaturesViewModel = new(listedItem);

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
            => Dispose();

        public override Task<bool> SaveChangesAsync()
        {
            return Task.FromResult(true);
        }

        public override void Dispose()
        {
            SignaturesViewModel.Dispose();
        }
    }
}
