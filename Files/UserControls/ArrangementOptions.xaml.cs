using Files.Enums;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class ArrangementOptions : UserControl
    {
        public ArrangementOptions()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty SelectedGroupModeProperty = DependencyProperty.Register(
         nameof(SelectedGroupMode),
         typeof(GroupOption),
         typeof(ArrangementOptions),
         new PropertyMetadata(null)
        );

        public GroupOption SelectedGroupMode
        {
            get => (GroupOption)GetValue(SelectedGroupModeProperty);
            set => SetValue(SelectedGroupModeProperty, value);
        }


        public static readonly DependencyProperty SelectedSortModeProperty = DependencyProperty.Register(
         nameof(SelectedSortMode),
         typeof(SortOption),
         typeof(ArrangementOptions),
         new PropertyMetadata(null)
        );

        public SortOption SelectedSortMode
        {
            get => (SortOption)GetValue(SelectedSortModeProperty);
            set => SetValue(SelectedSortModeProperty, value);
        }

        public static readonly DependencyProperty SelectedSortDirectionProperty = DependencyProperty.Register(
         nameof(SelectedSortDirection),
         typeof(SortDirection),
         typeof(ArrangementOptions),
         new PropertyMetadata(null)
        );

        public SortDirection SelectedSortDirection
        {
            get => (SortDirection)GetValue(SelectedSortDirectionProperty);
            set => SetValue(SelectedSortDirectionProperty, value);
        }

        public static readonly DependencyProperty IsPageTypeRecycleBinProperty = DependencyProperty.Register(
         nameof(IsPageTypeRecycleBin),
         typeof(bool),
         typeof(ArrangementOptions),
         new PropertyMetadata(null)
        );

        public bool IsPageTypeRecycleBin
        {
            get => (bool)GetValue(IsPageTypeRecycleBinProperty);
            set => SetValue(IsPageTypeRecycleBinProperty, value);
        }

        public static readonly DependencyProperty IsPageTypeCloudDriveProperty = DependencyProperty.Register(
         nameof(IsPageTypeCloudDrive),
         typeof(bool),
         typeof(ArrangementOptions),
         new PropertyMetadata(null)
        );

        public bool IsPageTypeCloudDrive
        {
            get => (bool)GetValue(IsPageTypeCloudDriveProperty);
            set => SetValue(IsPageTypeCloudDriveProperty, value);
        }
    }
}
