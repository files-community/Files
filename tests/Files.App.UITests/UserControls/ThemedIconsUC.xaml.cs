// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests.UserControls
{
    public sealed partial class ThemedIconsUC : UserControl
    {
        public ThemedIconsUC()
        {
            this.InitializeComponent();
        }

        void ButtonTestEnabledStates_Click(object sender, RoutedEventArgs e)
        {
            if (AppBarButtonDisable.IsEnabled)
                AppBarButtonDisable.IsEnabled = false;
            else if (!AppBarButtonDisable.IsEnabled)
                AppBarButtonDisable.IsEnabled = true;

            if (AppBarButtonDisable2.IsEnabled)
                AppBarButtonDisable2.IsEnabled = false;
            else if (!AppBarButtonDisable2.IsEnabled)
                AppBarButtonDisable2.IsEnabled = true;
        }
    }
}
